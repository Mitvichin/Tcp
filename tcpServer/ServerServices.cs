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
							Socket = handler
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
							clients.Add(handler, username);
							Send(handler, "Logged as " + user.Username);
						}
					}

					state.buffer = new byte[StateObject.BufferSize];
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

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));

					incomingBytes = Read(state, incomingBytes);
					content = state.sb.ToString();

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
					state.buffer = new byte[StateObject.BufferSize];
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


					incomingBytes = Read(state, incomingBytes);

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
				int bytes = Encoding.ASCII.GetByteCount(data);
				string length = bytes.ToString("0000000000");
				data = length + data;
				byte[] dataLength = Encoding.ASCII.GetBytes(data);

				handler.BeginSend(dataLength, 0, dataLength.Length, 0,
					new AsyncCallback(MessageDispatcher.SendMessage), handler);
			}
			catch (SocketException)
			{
				Console.WriteLine("Connection failure while sending!");
			}
		}

		//handles ordinary message
		private static void ProcessMessage(string message, StateObject state)

		{
			User user = users[clients[state.workSocket]];

			try
			{
				if (user.ConnectedTo != state.workSocket)
				{
					message = MessageBuilder("[", DateTime.Now.Date.ToString("dd/MM/yyyy"), "] ", clients[state.workSocket], ":", message);

					Send(user.ConnectedTo, message);
					CreateWriteChronology(message, user);
				}
				else
				{
					Send(state.workSocket, "Please, select user!(command:username/username)");
				}

				state.buffer = new byte[10];
				state.workSocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
						new AsyncCallback(ReadIncomingMessage), state);
			}
			catch (SocketException e)
			{
				CloseConnection(state, "Proccesing message failed!Connection problem occurred." + e.ToString());
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

					incomingBytes = Read(state, incomingBytes);
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
							Send(handler, MessageBuilder("Connecting to ", username, " failed!"));
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
			catch (SocketException)
			{
				CloseConnection(state, "Proccesing username failed! Connection problem occurred!");
			}
		}

		private static void SendImage(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;
			User user = users[clients[handler]];
			string imgBase64 = "";

			try
			{
				int incomingBytes = handler.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Append(Encoding.ASCII.GetString(
						state.buffer, 0, incomingBytes));

					incomingBytes = Read(state, incomingBytes);

					if (user.ConnectedTo != handler)
					{
						imgBase64 = state.sb.ToString();
						Send(user.ConnectedTo, imgBase64);
						Send(user.ConnectedTo, "Send image!");
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
			catch (SocketException)
			{
				CloseConnection(state, "Sending image failed! Connection problem occurred!");
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

					incomingBytes = Read(state, incomingBytes);

					username = state.sb.ToString();

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
					state.buffer = new byte[StateObject.BufferSize];
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

		public static void SendOnlineUsers(StateObject state)
		{
			Socket handler = state.workSocket;

			try
			{
				Send(handler, GetOnlineUsers());
				state.buffer = new byte[StateObject.BufferSize];
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

		public static string MessageBuilder(params string[] messageParts)
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

		private static int Read(StateObject state, int incomingBytes)
		{
			int bytesCount = -1;

			if (Int32.TryParse(state.sb.ToString(), out bytesCount))
			{
				state.buffer = new byte[bytesCount];
				state.sb.Clear();

				incomingBytes = state.workSocket.Receive(state.buffer, 0, state.buffer.Length, 0);
				state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, state.buffer.Length));
			}

			return incomingBytes;
		}
	}
}

