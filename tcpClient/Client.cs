using Models.Models;
using Shared;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using tcpServer;
using testTCP;

namespace tcpClient
{
	public class Client
	{
		private const int port = 11000;

		private static ManualResetEvent connectDone =
			new ManualResetEvent(false);
		public static ManualResetEvent sendDone =
			new ManualResetEvent(false);

		public static void StartClient()
		{
			try
			{
				IPHostEntry ipHostInfo = Dns.GetHostEntry("DESKTOP-587V0QC");
				IPAddress ipAddress = ipHostInfo.AddressList[0];
				IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

				Socket client = new Socket(ipAddress.AddressFamily,
					SocketType.Stream, ProtocolType.Tcp);

				client.BeginConnect(remoteEP,
					new AsyncCallback(ConnectCallback), client);
				connectDone.WaitOne();

				Receive(client);
				string username = Console.ReadLine();
				Send(client, username);

				while (true)
				{
					Console.WriteLine("input message!");
					string message = Console.ReadLine();
					switch (message)
					{
						default:
							Send(client, message);
							break;

						case "image/":
							Send(client, message);
							SendImageHandler imgHandler = new SendImageHandler(client);
							Thread dialogThread = new Thread(() => imgHandler.ShowDialog());
							dialogThread.SetApartmentState(ApartmentState.STA);
							dialogThread.Start();
							break;
					}
					sendDone.WaitOne();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		private static void ConnectCallback(IAsyncResult ar)
		{
			try
			{
				Socket client = (Socket)ar.AsyncState;

				client.EndConnect(ar);
				connectDone.Set();
			}
			catch (SocketException e)
			{
				string message = SharedMethods.MessageBuilder("While connecting issue occurred! Please, restart the program.",
					Environment.NewLine, "Press any key to exit the program!", Environment.NewLine, e.ToString());
				CloseApp(message);
			}
		}

		private static void Receive(Socket client)
		{
			try
			{
				StateObject state = new StateObject();
				state.workSocket = client;

				client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(ReceiveMessage), state);
			}
			catch (SocketException e)
			{
				string message = SharedMethods.MessageBuilder("While receiving a connection issue occurred! Please, restart the program.",
					Environment.NewLine, "Press any key to exit the program!", Environment.NewLine, e.ToString());
				CloseApp(message);
			}
		}

		private static void ReceiveMessage(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket client = state.workSocket;

			try
			{
				int incomingBytes = client.EndReceive(ar);

				if (incomingBytes > 0)
				{
					state.sb.Clear();
					state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, incomingBytes));
					SharedMethods.Read(state, incomingBytes);

					if (state.sb.Length > 1)
					{
						string message = state.sb.ToString();
						int commandEnd = message.IndexOf('*');
						string command = "";

						if (message.Contains("image*"))
						{
							command = message.Substring(0, commandEnd + 1);
						}

						switch (command)
						{
							default:
								Console.WriteLine(message);
								state.sb.Clear();
								state.buffer = new byte[StateObject.BufferSize];

								client.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
										new AsyncCallback(ReceiveMessage), state);
								break;

							case "image*":
								message = state.sb.ToString().Substring(commandEnd + 1);
								ReceiveImageHandler imgHandler = new ReceiveImageHandler(message);
								Thread dialogThread = new Thread(() => imgHandler.ShowDialog());
								dialogThread.SetApartmentState(ApartmentState.STA);
								dialogThread.Start();

								state.sb.Clear();
								state.buffer = new byte[StateObject.BufferSize];
								client.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
										new AsyncCallback(ReceiveMessage), state);
								break;
						}
					}
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException e)
			{
				string message = SharedMethods.MessageBuilder("While receiving a connection issue occurred! Please, restart the program.",
					Environment.NewLine, "Press any key to exit the program!", Environment.NewLine, e.ToString());
				CloseApp(message);
			}
		}

		public static void Send(Socket client, string data)
		{
			try
			{
				if (!string.IsNullOrEmpty(data))
				{
					int bytes = Encoding.ASCII.GetByteCount(data);
					string length = bytes.ToString("0000000000");
					data = length + data;
					byte[] dataLength = Encoding.ASCII.GetBytes(data);
					client.BeginSend(dataLength, 0, dataLength.Length, 0, new AsyncCallback(SendMessage), client);
					sendDone.Set();

				}
			}
			catch (SocketException e)
			{

				string message = SharedMethods.MessageBuilder("Connection failure while sending! Please, restart the program",
					Environment.NewLine, "Press any key to exit the program!", Environment.NewLine, e.ToString());
				CloseApp(message);
			}
		}

		private static void SendMessage(IAsyncResult ar)
		{
			try
			{
				Socket client = (Socket)ar.AsyncState;
				int bytesSent = client.EndSend(ar);

				sendDone.Set();
			}
			catch (SocketException e)
			{
				string message = SharedMethods.MessageBuilder("While sending a connection issue occurred! Please, restart the program",
					Environment.NewLine, "Press any key to exit the program!", Environment.NewLine, e.ToString());
				CloseApp(message);
			}
		}

		private static void CloseApp(string message)
		{
			Console.WriteLine(message);
			Console.ReadLine();
			Environment.Exit(0);
		}
	}
}
