using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace tcpServer
{
	public class User
	{
		public string Username { get; set; }
		public Socket Socket { get; set; }
		public Socket ConnectedTo { get; set; }
		public bool Online { get; set; }
	}
}
