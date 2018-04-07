using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
	public class SharedMethods
	{
		public static string MessageBuilder(params string[] messageParts)
		{
			StringBuilder sb = new StringBuilder();

			foreach (string part in messageParts)
			{
				sb.Append(part);
			}

			return sb.ToString();
		}
		public static void Read(StateObject state, int incomingBytes)
		{
			int bytesCount = -1;

			if (Int32.TryParse(state.sb.ToString(), out bytesCount))
			{
				state.buffer = new byte[bytesCount];
				state.sb.Clear();

				incomingBytes = state.workSocket.Receive(state.buffer, 0, state.workSocket.Available, 0);
				state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, incomingBytes));

				if (incomingBytes < state.buffer.Length)
				{
					ReadTillTheEnd(state, incomingBytes, state.buffer.Length);
				}

			}
		}

		private static void ReadTillTheEnd(StateObject state, int incomingBytes, int bufferSize)
		{
			do
			{
				int availableBytes = state.workSocket.Available;
				incomingBytes += state.workSocket.Receive(state.buffer, 0, state.workSocket.Available, 0);
				state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, availableBytes));
				string smtasdas = state.sb.ToString();
			}
			while (state.workSocket.Available > 0);
		}
	}

}
