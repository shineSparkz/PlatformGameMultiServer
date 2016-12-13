using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace Server.Server
{
	public class GameClient
	{
		public Socket tcpSocket = null;
		public UdpClient udpSocket = null;

		public IPEndPoint udpRemoteEndpoint = null;
		public int udpLocalPort;
		public int playerObjectHandle;

		public GameClient(Socket tcp, UdpClient udp, IPEndPoint ep)
		{
			this.tcpSocket = tcp;
			this.udpSocket = udp;
			this.udpRemoteEndpoint = ep;
			this.udpLocalPort = -1;
		}
	}
}
