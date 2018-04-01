using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace tcpServer
{

	public class Program
	{


		public static int Main(String[] args)
		{
			MessageDispatcher.StartListening();
			return 0;
		}
	}
}
