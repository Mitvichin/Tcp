﻿using System.Net.Sockets;
using System.Text;

namespace Models.Models
{
	public class StateObject
	{
		public Socket workSocket = null;
		public const int BufferSize = 10;
		public byte[] buffer = new byte[BufferSize];
		public StringBuilder sb = new StringBuilder();
	}
}
