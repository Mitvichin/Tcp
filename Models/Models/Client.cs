using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Models.Models
{
	public class Client
	{
		public string Username { get; set; }
		public Socket Socket { get; set; }
	}
}
