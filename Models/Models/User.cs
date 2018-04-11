using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Models.Models
{
	public class User
	{
		public string Username { get; set; }
		public Socket Socket { get; set; }
		public Socket ConnectedTo { get; set; }
		public bool Online { get; set; }
		public Dictionary<string, StringBuilder> OfflineMessages { get; set; }
	}
}
