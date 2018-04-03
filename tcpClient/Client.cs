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
		private static ManualResetEvent sendDone =
			new ManualResetEvent(false);
		private static ManualResetEvent receiveDone =
			new ManualResetEvent(false);

		private static string imgData = String.Empty;


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
				Console.WriteLine("While connecting issue occurred! Please, restart the program." + e.ToString());
				Console.WriteLine("Press any key to exit the program!");
				Console.ReadLine();
				Environment.Exit(0);
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
				Console.WriteLine("While receiving a connection issue occurred! Please, restart the program." + e.ToString());
				Console.WriteLine("Press any key to exit the program!");
				Console.ReadLine();
				Environment.Exit(0);
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

					int bytesCount = -1;

					if (Int32.TryParse(state.sb.ToString(), out bytesCount))
					{
						state.buffer = new byte[bytesCount];
						state.sb.Clear();

						state.workSocket.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
							new AsyncCallback(ReceiveMessage), state);
						return;
					}

					if (state.sb.Length > 1)
					{
						Console.WriteLine(state.sb.ToString());
						state.sb.Clear();
						state.buffer = new byte[StateObject.BufferSize];

						client.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
								new AsyncCallback(ReceiveMessage), state);
					}
				}
				else
				{
					throw new SocketException();
				}
			}
			catch (SocketException e)
			{
				Console.WriteLine("While receiving a connection issue occurred! Please, restart the program." + e.ToString());
				Console.WriteLine("Press any key to exit the program!");
				Console.ReadLine();
				Environment.Exit(0);
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

				}
			}
			catch (SocketException e)
			{
				Console.WriteLine("Connection failure!(at send)");
				Console.WriteLine("Press any key to exit the program!" + e.ToString());
				Console.ReadLine();
				Environment.Exit(0);
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
				Console.WriteLine("While sending a connection issue occurred! Please, restart the program");
				Console.WriteLine("Press any key to exit the program!" + e.ToString());
				Console.ReadLine();
				Environment.Exit(0);
			}
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
