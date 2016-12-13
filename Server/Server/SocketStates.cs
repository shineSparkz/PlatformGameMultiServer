using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace Server.Server
{
	public class TcpStateObject
	{
		public Socket tcpSocket = null;
		public byte[] buffer = new byte[ServerDefs.BUFF_SIZE];
		public StringBuilder strBldr = new StringBuilder();
	}

	public class UdpStateObject
	{
		public UdpClient udpSocket;
		public IPEndPoint endPoint;
	}
}
