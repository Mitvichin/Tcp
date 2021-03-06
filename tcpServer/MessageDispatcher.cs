﻿using Models.Models;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tcpServer
{
	public class MessageDispatcher
	{
		public static ManualResetEvent allDone = new ManualResetEvent(false);

		public static void StartListening()
		{
			IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddress = ipHostInfo.AddressList[0];
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

			Socket listener = new Socket(ipAddress.AddressFamily,
				SocketType.Stream, ProtocolType.Tcp);

			try
			{
				listener.Bind(localEndPoint);
				listener.Listen(100);

				while (true)
				{
					allDone.Reset();

					Console.WriteLine("Waiting for a connection...");
					listener.BeginAccept(
						new AsyncCallback(AcceptCallback),
						listener);

					allDone.WaitOne();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			Console.WriteLine("\nPress ENTER to continue...");
			Console.Read();

		}

		private static void AcceptCallback(IAsyncResult ar)
		{
			Socket listener = (Socket)ar.AsyncState;
			Socket handler = listener.EndAccept(ar);

			allDone.Set();

			try
			{
				ServerServices.Send(handler, SharedMethods.MessageBuilder("Commands: username/username, all/message, chronology/username, onlineUsers/, offlineUsers/, checkOfflineMessages/username, image/" +
					Environment.NewLine, "Online users: ", ServerServices.GetUsers(true),
					Environment.NewLine, "Press enter after command and then write the variable for the command!!!", Environment.NewLine, "Connected! Please choose your username:"));
				StateObject state = new StateObject();
				state.workSocket = handler;
				handler.BeginReceive(state.buffer, 0, state.buffer.Length, 0, new AsyncCallback(ServerServices.SignIn), state);
			}
			catch (ObjectDisposedException)
			{
				Console.WriteLine("Connection failure!");
			}
		}

		public static void SendMessage(IAsyncResult ar)
		{
			try
			{
				Socket handler = (Socket)ar.AsyncState;
				int bytesSent = handler.EndSend(ar);
				Console.WriteLine(bytesSent.ToString());
			}
			catch (SocketException e)
			{
				Console.WriteLine("Connection error occured while sending message!" + e.ToString());
			}
		}


	}
}
