using Models.Models;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace tcpServer
{
	public static class ServerServices
	{

		private static Dictionary<string, User> users = new Dictionary<string, User>();
		private static Dictionary<Socket, string> clients = new Dictionary<Socket, string>();
		private static Dictionary<string, StringBuilder> chronologies = new Dictionary<string, StringBuilder>();
		private static string basePath = @"C:\TCPServer\";
		public static ManualResetEvent sendDone = new ManualResetEvent(false);

		//register or log in
		public static void SignIn(IAsyncResult ar)
		{
			string username = "";
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;

			try
			{
				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));

					int bytesCount = -1;

					if (Int32.TryParse(state.sb.ToString(), out bytesCount))
					{
						state.buffer = new byte[bytesCount];
						state.sb.Clear();

						state.workSocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
							new AsyncCallback(SignIn), state);
						return;
					}

					if (state.buffer.Length == incomingBytes)
					{
						username = state.sb.ToString();
						state.sb.Clear();
						User user = new User()
						{
							Username = username,
							ConnectedTo = handler,
							Online = true,
							Socket = handler,
							OfflineMessages = new Dictionary<string, StringBuilder>()

						};

						if (!users.ContainsKey(user.Username))
						{
							users.Add(username, user);
							clients.Add(handler, username);
							Send(handler, "Registered!");
						}
						else if (users[user.Username].Online)
						{
							Send(handler, "Username already exists! Please, choose another.");
							handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
								new AsyncCallback(SignIn), state);
							return;
						}
						else
						{
							users[user.Username].Online = true;
							users[user.Username].ConnectedTo = handler;
							users[user.Username].Socket = handler;
							if (clients.ContainsValue(user.Username))
							{
								Socket oldSocket = clients.FirstOrDefault(c => c.Value.Equals(user.Username)).Key;
								clients.Remove(oldSocket);
							}
							clients.Add(handler, username);
							Send(handler, "Logged as " + user.Username);
						}
					}

					state.buffer = new byte[StateObject.BufferSize];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
							new AsyncCallback(ReadIncomingMessage), state);
				}

			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Registration or signing in failed! Connection problem occurred.", Environment.NewLine, e.ToString()));
			}
		}

		private static void SendToAll(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;
			string content = "";

			try
			{
				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));

					SharedMethods.Read(state, incomingBytes);
					content = state.sb.ToString();

					string message = SharedMethods.MessageBuilder("[", DateTime.Now.Date.ToString("dd/MM/yyyy"), "][ALL] ", clients[handler], ":", content);

					User[] userCollection = new User[users.Count];
					users.Values.CopyTo(userCollection, 0);

					foreach (User user in userCollection)
					{
						if (user.Online && user.Socket != handler)
						{
							Send(user.Socket, message);
						}
					}
					state.sb.Clear();
					state.buffer = new byte[StateObject.BufferSize];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
							new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}

			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Send to all failed! Connection problem occurred.", Environment.NewLine, e.ToString()));
			}
		}

		public static void ReadIncomingMessage(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			state.sb.Clear();
			Socket handler = state.workSocket;

			try
			{
				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));


					SharedMethods.Read(state, incomingBytes);

					string message = state.sb.ToString();


					state.sb.Clear();
					state.buffer = new byte[StateObject.BufferSize];
					switch (message)
					{
						default:
							ProcessMessage(message, state);
							return;

						case "username/":
							handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
									new AsyncCallback(ConnectTwoClients), state);
							return;

						case "all/":
							handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
									new AsyncCallback(SendToAll), state);
							return;

						case "chronology/":
							handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
								new AsyncCallback(RetrieveChronology), state);
							return;
						case "onlineUsers/":
							SendOnlineUsers(state);
							return;

						case "image/":
							handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
								new AsyncCallback(SendImage), state);
							return;
						case "offlineUsers/":
							SendOfflineUsers(state);
							return;
						case "checkOfflineMessages/":
							handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
								new AsyncCallback(SendOfflineMessages), state);
							return;
					}
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Reading message failed! Connection problem occurred.", Environment.NewLine, e.ToString()));
			}
		}

		private static void SendOfflineMessages(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;
			User user = users[clients[handler]];
			try
			{

				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
							state.buffer, 0, incomingBytes));

					SharedMethods.Read(state, incomingBytes);

					string username = state.sb.ToString();

					if (user.OfflineMessages.ContainsKey(username))
					{
						Send(handler, SharedMethods.MessageBuilder("Offline messages: ", user.OfflineMessages[username].ToString()));
						user.OfflineMessages.Remove(username);
					}
					else
					{
						Send(handler, "You don't have messages from this user!");
					}

					state.sb.Clear();
					state.buffer = new byte[StateObject.BufferSize];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
					 new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Connection problem occurred while checking offline messages!", Environment.NewLine, e.ToString()));
			}
		}

		private static void SendOfflineUsers(StateObject state)
		{
			Socket handler = state.workSocket;

			try
			{
				Send(handler, SharedMethods.MessageBuilder("Offline users: ", GetUsers(false)));
				state.buffer = new byte[StateObject.BufferSize];

				handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
				 new AsyncCallback(ReadIncomingMessage), state);
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Connection problem occurred while geting online users!", Environment.NewLine, e.ToString()));
			}
		}

		public static void Send(Socket handler, string data)
		{
			StateObject state = new StateObject() { buffer = new byte[10], sb = new StringBuilder(), workSocket = handler };

			try
			{
				int bytes = Encoding.ASCII.GetByteCount(data);
				string length = bytes.ToString("0000000000");
				data = length + data;
				byte[] dataLength = Encoding.ASCII.GetBytes(data);

				handler.BeginSend(dataLength, 0, dataLength.Length, 0,
					new AsyncCallback(MessageDispatcher.SendMessage), handler);
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Connection failure while sending!", Environment.NewLine, e.ToString()));
			}
		}

		//handles ordinary message
		private static void ProcessMessage(string message, StateObject state)
		{
			User user = users[clients[state.workSocket]];

			try
			{
				if (user.ConnectedTo != state.workSocket && clients.ContainsKey(user.ConnectedTo))
				{
					message = SharedMethods.MessageBuilder("[", DateTime.Now.Date.ToString("dd/MM/yyyy"), "] ", clients[state.workSocket], ":", message);

					if (!user.ConnectedTo.Connected)
					{
						User connectedUser = users.FirstOrDefault(u => u.Value.Username.Equals(clients[user.ConnectedTo])).Value;

						if (connectedUser.OfflineMessages.ContainsKey(clients[state.workSocket]))
						{
							connectedUser.OfflineMessages[clients[state.workSocket]].Append(SharedMethods.MessageBuilder(message, Environment.NewLine));
						}
						else
						{
							connectedUser.OfflineMessages.Add(clients[state.workSocket], new StringBuilder());
							connectedUser.OfflineMessages[clients[state.workSocket]].Append(SharedMethods.MessageBuilder(message, Environment.NewLine));
						}
						Send(user.Socket, "The user you are trying to message is currently offline and will receive the message once he is online!");
					}
					else
					{
						Send(user.ConnectedTo, message);
					}
					CreateWriteChronology(message, user);
				}
				else
				{
					Send(state.workSocket, "Please, select user!(command:username/username)");
				}

				state.buffer = new byte[StateObject.BufferSize];
				state.workSocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
						new AsyncCallback(ReadIncomingMessage), state);
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Proccesing message failed!Connection problem occurred.", Environment.NewLine, e.ToString()));
			}
		}

		//connects two people
		public static void ConnectTwoClients(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;
			string username = "";

			try
			{
				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));

					SharedMethods.Read(state, incomingBytes);
					username = state.sb.ToString();


					if (users.ContainsKey(username))
					{
						string currentUser = clients[handler];

						if (users[currentUser].ConnectedTo != users[username].Socket)
						{
							users[currentUser].ConnectedTo = users[username].Socket;
							Send(handler, "Connected");
						}
						else
						{
							Send(handler, SharedMethods.MessageBuilder("Connecting to ", username, " failed!"));
						}
					}
					else
					{
						Send(handler, "User doesn't exit!");
					}

					state.sb.Clear();
					state.buffer = new byte[StateObject.BufferSize];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
					 new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Proccesing username failed! Connection problem occurred!", Environment.NewLine, e.ToString()));
			}
		}

		private static void SendImage(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;
			User user = users[clients[state.workSocket]];
			string imgBase64 = "";

			try
			{
				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));

					SharedMethods.Read(state, incomingBytes);

					if (user.ConnectedTo != handler)
					{
						imgBase64 = state.sb.ToString();
						Send(user.ConnectedTo, SharedMethods.MessageBuilder("[", DateTime.Now.Date.ToString("dd/MM/yyyy"), "] ", user.Username, " send image!"));
						Send(user.ConnectedTo, "image*" + imgBase64);
						CreateWriteChronology("Send image!", user);
					}
					else
					{
						Send(handler, "Please, select user!(command:username/username)");
					}

					state.sb.Clear();
					state.buffer = new byte[StateObject.BufferSize];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
					 new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Sending image failed! Connection problem occurred!", Environment.NewLine, e.ToString()));
			}

		}

		//creates chronology or writes in existing one 
		private static void CreateWriteChronology(string message, User user)
		{
			string currentChronology = "";
			string path = basePath + @"Chronologies\";
			string[] chronologyNames = Directory.EnumerateFiles(path).Select(Path.GetFileName).ToArray();

			foreach (string name in chronologyNames)
			{
				if (name.Contains(user.Username) && name.Contains(clients[user.ConnectedTo]))
				{
					currentChronology = name;
				}
			}

			if (String.IsNullOrEmpty(currentChronology) && !chronologies.ContainsKey(currentChronology))
			{
				currentChronology = SharedMethods.MessageBuilder(user.Username, "_", clients[user.ConnectedTo]);
				chronologies.Add(currentChronology, new StringBuilder());
			}

			//useless without inmemory
			if (!chronologies.ContainsKey(currentChronology))
			{
				chronologies.Add(currentChronology, new StringBuilder());
			}

			chronologies[currentChronology].Append(SharedMethods.MessageBuilder(message, Environment.NewLine));

			if (chronologies[currentChronology].Length > 20)
			{
				string filePath = path + currentChronology;

				File.AppendAllText(filePath, chronologies[currentChronology].ToString());
				chronologies[currentChronology].Clear();
			}

		}

		public static void RetrieveChronology(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;
			string path = SharedMethods.MessageBuilder(basePath, @"Chronologies\");
			string[] chronologyNames = Directory.EnumerateFiles(path).Select(Path.GetFileName).ToArray();
			User user = users[clients[handler]];
			string username = "";
			string chronologyName = "";
			string chronologyContent = "";

			try
			{
				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));

					SharedMethods.Read(state, incomingBytes);

					username = state.sb.ToString();

					foreach (string name in chronologyNames)
					{
						if (name.Contains(user.Username) && name.Contains(username))
						{
							chronologyName = name;
						}
					}

					if (File.Exists(SharedMethods.MessageBuilder(path, chronologyName)))
					{
						chronologyContent = File.ReadAllText(SharedMethods.MessageBuilder(path, chronologyName));
						Send(handler, SharedMethods.MessageBuilder("CHRONOLOGY: ", Environment.NewLine, chronologyContent));
					}
					else
					{
						Send(handler, "Chronology doesn't exist!");
					}

					state.sb.Clear();
					state.buffer = new byte[StateObject.BufferSize];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
					 new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Proccesing username failed! Connection problem occurred!", Environment.NewLine, e.ToString()));
			}
		}

		public static void SendOnlineUsers(StateObject state)
		{
			Socket handler = state.workSocket;

			try
			{
				Send(handler, SharedMethods.MessageBuilder("Online users: ", GetUsers(true)));
				state.buffer = new byte[StateObject.BufferSize];
				handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
				 new AsyncCallback(ReadIncomingMessage), state);
			}
			catch (SocketException e)
			{
				CloseConnection(state, SharedMethods.MessageBuilder("Connection problem occurred while geting online users!", Environment.NewLine, e.ToString()));
			}
		}

		public static void CloseConnection(StateObject state, string message)
		{
			if (state.workSocket.Connected)
			{
				state.workSocket.Shutdown(SocketShutdown.Both);
				state.workSocket.Close();
			}

			foreach (User user in users.Values)
			{
				if (user.ConnectedTo.Equals(state.workSocket) && user.Username != clients[state.workSocket])
				{
					user.ConnectedTo = user.Socket;
					if (user.Online)
					{
						Send(user.Socket, SharedMethods.MessageBuilder(clients[state.workSocket], " has disconnected!"));
					}
				}
			}

			if (clients.ContainsKey(state.workSocket) && users.ContainsKey(clients[state.workSocket]))
			{
				users[clients[state.workSocket]].Online = false;
			}

			Console.WriteLine(message);
		}



		public static string GetUsers(bool userState)
		{
			string userNames = "";

			foreach (User user in users.Values)
			{
				if (user.Online == userState)
				{
					userNames += SharedMethods.MessageBuilder(user.Username);
				}
			}

			return userNames;
		}


	}
}

