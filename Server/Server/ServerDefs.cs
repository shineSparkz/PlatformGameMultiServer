using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace Server.Server
{
	// Shared Hardcoded definitions
	public static class ServerDefs
	{
		public const int BUFF_SIZE = 512;
		public const int TCP_LISTEN_PORT = 28000;
		public const int BROADCAST_PORT = 8081;

		public static string GetLocalIPAddress()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			for (int ip = host.AddressList.Length - 1; ip >= 0; --ip)
			{
				if (host.AddressList[ip].AddressFamily == AddressFamily.InterNetwork)
				{
					return host.AddressList[ip].ToString();
				}
			}
			throw new Exception("Local IP Address Not Found!");
		}
	};
}
