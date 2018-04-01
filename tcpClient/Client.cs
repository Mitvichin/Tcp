using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using tcpServer;

namespace tcpClient
{
	public static class Client
	{
		private const int port = 11000;

		private static ManualResetEvent connectDone =
			new ManualResetEvent(false);
		private static ManualResetEvent sendDone =
			new ManualResetEvent(false);
		private static ManualResetEvent receiveDone =
			new ManualResetEvent(false);

		private static String response = String.Empty;

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
					Send(client, message);
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
				Console.WriteLine(e.ToString());
			}
		}

		private static void Receive(Socket client)
		{
			try
			{
				StateObject state = new StateObject();
				state.workSocket = client;
				response = string.Empty;

				client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback(ReceiveCallback), state);
			}
			catch (SocketException)
			{
				Console.WriteLine("While receiving a connection issue occurred! Please, restart the program.");
			}
		}

		private static void ReceiveCallback(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket client = state.workSocket;

			try
			{
				int bytesRead = client.EndReceive(ar);

				if (bytesRead > 0)
				{
					state.sb.Clear();
					state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
					string message = (state.sb.ToString());

					if (message.IndexOf('@') > -1)
					{
						int bufferSize = int.Parse(message.Split('@')[0]);
						state.buffer = new byte[bufferSize];
						state.sb.Clear();
						client.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
							new AsyncCallback(ReceiveCallback), state);
					}



					if (state.sb.Length > 1)
					{
						response = string.Empty;
						response = state.sb.ToString();
						receiveDone.Set();
						Console.WriteLine(response);
						state.sb.Clear();
						client.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
								new AsyncCallback(ReceiveCallback), state);
					}
				}
			}
			catch (SocketException)
			{
				Console.WriteLine("While receiving a connection issue occurred! Please, restart the program.");
				Console.WriteLine("Press any key to exit the program!");
				Console.ReadLine();
				Environment.Exit(0);
			}
		}

		private static void Send(Socket client, string data)
		{
			try
			{
				if (!string.IsNullOrEmpty(data))
				{

					int bytes = Encoding.ASCII.GetByteCount(data);
					int commandEnd = data.IndexOf('/');
					string messageDetails = bytes.ToString() + "@";

					if (commandEnd > 0)
					{
						messageDetails = messageDetails + data.Substring(0, commandEnd);
					}


					byte[] dataLength = Encoding.ASCII.GetBytes(messageDetails);
					client.BeginSend(dataLength, 0, dataLength.Length, 0, new AsyncCallback(SendCallback), client);

					sendDone.WaitOne();

					byte[] byteData = Encoding.ASCII.GetBytes(data);
					client.BeginSend(byteData, 0, byteData.Length, 0,
						new AsyncCallback(SendCallback), client);
				}
			}
			catch (SocketException)
			{
				Console.WriteLine("Connection failure!(at send)");
				Console.WriteLine("Press any key to exit the program!");
				Console.ReadLine();
				Environment.Exit(0);
			}
		}

		private static void SendCallback(IAsyncResult ar)
		{
			try
			{
				Socket client = (Socket)ar.AsyncState;
				int bytesSent = client.EndSend(ar);
				Console.WriteLine("Sent {0} bytes to server.", bytesSent);

				sendDone.Set();
			}
			catch (SocketException)
			{
				Console.WriteLine("While sending a connection issue occurred! Please, restart the program");
				Console.WriteLine("Press any key to exit the program!");
				Console.ReadLine();
				Environment.Exit(0);
			}
		}
	}
}
