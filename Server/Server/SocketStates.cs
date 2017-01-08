using System.Net;
using System.Net.Sockets;

namespace Server.Server
{
	public class TcpStateObject
	{
		public Socket tcpSocket = null;
		public byte[] buffer = new byte[ServerDefs.BUFF_SIZE];
	}

	public class UdpStateObject
	{
		public UdpClient udpSocket;
		public IPEndPoint endPoint;
	}
}
