using System;

namespace tcpClient
{
	public class Program
	{
		public static int Main(String[] args)
		{
			Client.StartClient();
			Console.ReadLine();
			return 0;
		}
	}
}