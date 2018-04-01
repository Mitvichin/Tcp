using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tcpServer
{
	public static class ServerServices
	{

		private static Dictionary<string, User> users = new Dictionary<string, User>();
		private static Dictionary<Socket, string> clients = new Dictionary<Socket, string>();
		private static Dictionary<string, StringBuilder> chronologies = new Dictionary<string, StringBuilder>();
		private static string basePath = @"C:\TCPServer\";

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

					string bytesCount = (state.sb.ToString());

					if (bytesCount.IndexOf('@') > -1)
					{
						bytesCount = bytesCount.Split('@')[0];
						state.buffer = new byte[int.Parse(bytesCount)];
						state.sb.Clear();

						handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
								new AsyncCallback(SignIn), state);
						return;
					}

					if (state.buffer.Length == incomingBytes)
					{
						username = state.sb.ToString();
						state.sb.Clear();
						User user = new User() { Username = username, ConnectedTo = handler, Online = true, Socket = handler };

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
							clients.Add(handler, username);
							Send(handler, "Logged as " + user.Username);
						}
					}
					state.buffer = new byte[20];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
							new AsyncCallback(ReadIncomingMessage), state);
				}

			}
			catch (SocketException)
			{
				CloseConnection(state, "Registration or signing in failed! Connection problem occurred.");
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

				if (incomingBytes > 0 && state.buffer.Length == incomingBytes)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));
					content = state.sb.ToString().Split('/')[1];

					string message = MessageBuilder("[", DateTime.Now.Date.ToString("dd/MM/yyyy"), "][ALL] ", clients[handler], ":", content);

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
					state.buffer = new byte[20];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
							new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}

			}
			catch (SocketException)
			{
				CloseConnection(state, "Send to all failed! Connection problem occurred.");

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

					string message = (state.sb.ToString());

					if (message.IndexOf('@') > -1)
					{
						string messageLength = message.Split('@')[0];
						message = message.Split('@')[1];
						state.buffer = new byte[int.Parse(messageLength)];
						state.sb.Clear();

						switch (message)
						{
							default:
								handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
										new AsyncCallback(ProcessMessage), state);
								return;

							case "username":
								handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
										new AsyncCallback(ConnectTwoClients), state);
								return;

							case "all":
								handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
										new AsyncCallback(SendToAll), state);
								return;

							case "chronology":
								handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
									new AsyncCallback(RetrieveChronology), state);
								return;
							case "onlineUsers":
								handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
									new AsyncCallback(SendOnlineUsers), state);
								return;
						}
					}
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException)
			{
				CloseConnection(state, "Reading message failed! Connection problem occurred.");
			}
		}

		public static void Send(Socket handler, string data)
		{
			try
			{
				string dataLength = Encoding.ASCII.GetByteCount(data).ToString() + '@';
				byte[] byteData = Encoding.ASCII.GetBytes(data);

				byte[] byteDataLength = Encoding.ASCII.GetBytes(dataLength);
				handler.BeginSend(byteDataLength, 0, byteDataLength.Length, 0,
					new AsyncCallback(MessageDispatcher.SendMessage), handler);

				MessageDispatcher.sendDone.WaitOne();

				handler.BeginSend(byteData, 0, byteData.Length, 0,
						new AsyncCallback(MessageDispatcher.SendMessage), handler);
			}
			catch (SocketException)
			{
				Console.WriteLine("Connection failure while sending!");
			}
		}

		//handles ordinary message
		private static void ProcessMessage(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;
			User user = users[clients[handler]];
			string content = "";

			try
			{
				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0 && state.buffer.Length == incomingBytes)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));
					content = state.sb.ToString();

					Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
						content.Length, content);

					if (user.ConnectedTo != handler)
					{
						string message = MessageBuilder("[", DateTime.Now.Date.ToString("dd/MM/yyyy"), "] ", clients[handler], ":", content);

						Send(user.ConnectedTo, message);
						CreateChronology(message, user);
					}
					else
					{
						Send(handler, "Please, select user!(command:username/username)");
					}

					state.buffer = new byte[20];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
							new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException)
			{
				CloseConnection(state, "Proccesing message failed!Connection problem occurred.");
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
					username = state.sb.ToString().Split('/')[1];

					string currentUser = clients[handler];

					if (users.ContainsKey(username))
					{
						if (users[currentUser].ConnectedTo != users[username].Socket)
						{
							users[currentUser].ConnectedTo = users[username].Socket;
							Send(handler, "Connected");
						}
						else
						{
							Send(handler, MessageBuilder("Connecting to ", username, " failed!"));
						}
					}
					else
					{
						Send(handler, "User doesn't exit!");
					}

					state.sb.Clear();
					state.buffer = new byte[20];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
					 new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException)
			{
				CloseConnection(state, "Proccesing username failed! Connection problem occurred!");
			}
		}

		//creates chronology or writes in existing one 
		private static void CreateChronology(string message, User user)
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
				currentChronology = MessageBuilder(user.Username, "_", clients[user.ConnectedTo]);
				chronologies.Add(currentChronology, new StringBuilder());
			}

			//useless without inmemory
			if (!chronologies.ContainsKey(currentChronology))
			{
				chronologies.Add(currentChronology, new StringBuilder());
			}

			chronologies[currentChronology].Append(MessageBuilder(message, Environment.NewLine));

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
			string path = MessageBuilder(basePath, @"Chronologies\");
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
					username = state.sb.ToString().Split('/')[1];

					foreach (string name in chronologyNames)
					{
						if (name.Contains(user.Username) && name.Contains(username))
						{
							chronologyName = name;
						}
					}

					if (File.Exists(MessageBuilder(path, chronologyName)))
					{
						chronologyContent = File.ReadAllText(MessageBuilder(path, chronologyName));
						Send(handler, MessageBuilder("CHRONOLOGY: ", Environment.NewLine, chronologyContent));
					}
					else
					{
						Send(handler, "Chronology doesn't exist!");
					}

					state.sb.Clear();
					state.buffer = new byte[20];
					handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
					 new AsyncCallback(ReadIncomingMessage), state);
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException)
			{
				CloseConnection(state, "Proccesing username failed! Connection problem occurred!");
			}
		}

		public static void SendOnlineUsers(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;

			try
			{
				Send(handler, GetOnlineUsers());
				state.buffer = new byte[20];
				handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
				 new AsyncCallback(ReadIncomingMessage), state);
			}
			catch (SocketException e)
			{
				CloseConnection(state, "Connection problem occurred while geting online users!" + e.ToString());
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
				if (user.ConnectedTo.Equals(state.workSocket))
				{
					user.ConnectedTo = user.Socket;
					if (user.Online)
					{
						Send(user.Socket, MessageBuilder(clients[state.workSocket], " has disconnected!"));
					}
				}
			}

			if (clients.ContainsKey(state.workSocket) && users.ContainsKey(clients[state.workSocket]))
			{
				users[clients[state.workSocket]].Online = false;
				clients.Remove(state.workSocket);
			}


			Console.WriteLine(message);
		}

		private static string MessageBuilder(params string[] messageParts)
		{
			StringBuilder sb = new StringBuilder();

			foreach (string part in messageParts)
			{
				sb.Append(part);
			}

			return sb.ToString();
		}

		public static string GetOnlineUsers()
		{
			string onlineUsers = "Online users: ";

			foreach (User user in users.Values)
			{
				if (user.Online)
				{
					onlineUsers += user.Username + ", ";
				}
			}

			return onlineUsers;
		}
	}
}
