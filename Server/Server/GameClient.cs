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
		// Net stuff
		public Socket tcpSocket = null;
		public UdpClient udpSocket = null;
		public IPEndPoint udpRemoteEndpoint = null;
		public int udpLocalPort;

		// Game specific
		public bool loggedIn = false;
        public bool inGame = false;
		public int playerObjectHandle = -1;
		public string userName = "";
        public int localExpCache = 0;   // Minimise database hits

		public GameClient(Socket tcp, UdpClient udp, IPEndPoint ep)
		{
			this.tcpSocket = tcp;
			this.udpSocket = udp;
			this.udpRemoteEndpoint = ep;
			this.udpLocalPort = -1;
		}
	}
}
