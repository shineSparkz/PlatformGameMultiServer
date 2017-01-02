using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Server.Utils;
using Server.GameSpecific;

namespace Server.Server
{
	public class AsynchSocketListener
	{
		// Thread signal.
		public ManualResetEvent allDone = new ManualResetEvent(false);
		private ServerManager m_ServerManager = null;
		private GameSimulation m_GameSimulation = null;
		private MessageParser m_MessageParser = null;

		public AsynchSocketListener()
		{
            m_ServerManager = ServerManager.instance;
            m_GameSimulation = GameSimulation.instance;

			m_MessageParser = new MessageParser(m_GameSimulation, m_ServerManager);
		}

		public void StartListeningForNewClients()
		{
			// Grab our local Ip Address that we have sent out in any connected client 
			string localIp = ServerDefs.GetLocalIPAddress();

			Logger.Log(string.Format("Starting to listen for tcp connection on: {0}", localIp));

			// Local End point for listening to new TCP connection
			IPEndPoint addr = new IPEndPoint(IPAddress.Parse(localIp), ServerDefs.TCP_LISTEN_PORT);
			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			// Non-blocking reactor pattern for new clients joining the server
			try
			{
				listener.Bind(addr);
				listener.Listen(100);

				while (true)
				{
					allDone.Reset();

					// Got a new connection
					listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
					allDone.WaitOne();
				}
			}
			catch (Exception e)
			{
				Logger.Log(string.Format("Exception handled : {0}", e.Message), Logger.LogPrio.Error);
			}
		}

		private void AcceptCallback(IAsyncResult ar)
		{
			allDone.Set();

			// ---- Set up tcp socket ----
			Socket listener = (Socket)ar.AsyncState;
			Socket localTcp = listener.EndAccept(ar);

			// ---- Generate a state object to pass around Async calls ----
			TcpStateObject tcpStateObj = new TcpStateObject();
			tcpStateObj.tcpSocket = localTcp;

			// ---- Add a new client with this Tcp socket and get the hash ----
			int hash = m_ServerManager.AddNewClient(tcpStateObj.tcpSocket);

			// ---- Gen a Udp state object on free system port----
			IPEndPoint localUpdEndPt = new IPEndPoint(IPAddress.Parse(ServerDefs.GetLocalIPAddress()), 0); 
			UdpClient localUdp = new UdpClient(localUpdEndPt);                     

			UdpStateObject udpStateObj = new UdpStateObject();
			udpStateObj.endPoint = localUpdEndPt;
			udpStateObj.udpSocket = localUdp;

			// Resolve system generated port of local udp socket
			int port = ((IPEndPoint)localUdp.Client.LocalEndPoint).Port;
			Logger.Log(string.Format("Listening for UDP connection on port {0}", port));

			// Store this in client data
			m_ServerManager.SetLocalUdpPort(hash, port);

			// **Important -- SEND Register Function and give them their id which they must store, and use in all packets so we know who they are, also send them local udp port that we are listening for them on.
			m_ServerManager.SendTcp(tcpStateObj.tcpSocket, fastJSON.JSON.ToJSON(new PacketDefs.regPacket(hash, port), PacketDefs.JsonParams()));

			// ---- Begin Async Tcp receive for this client
			tcpStateObj.tcpSocket.BeginReceive(tcpStateObj.buffer, 0,                                     
				ServerDefs.BUFF_SIZE, 0,                                     
				new AsyncCallback(TcpReadCallback), tcpStateObj                            
			);

			// ----  The client knows this local udp port, so if/when we get a message from them on this, we can add their end
			//		 point to the client hash table and connect a local udp Socket to that end point which is also stored and
			//		 completes the data structure for this client
			localUdp.BeginReceive(new AsyncCallback(UdpInitialReadCallback), udpStateObj);

			Logger.Log("New client connected to server");
			Logger.Log(string.Format("Number of clients connected {0}.\nNew client TCP Endpoint {1}.\nWaiting for udp connection on port {2}",
				m_ServerManager.NumClients(), localTcp.RemoteEndPoint, port));
		}

		public void TcpReadCallback(IAsyncResult ar)
		{
			TcpStateObject state = (TcpStateObject)ar.AsyncState;
			Socket tcp_socket = state.tcpSocket;
			int bytesRead = 0;

			try
			{
				bytesRead = tcp_socket.EndReceive(ar);
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception handled : {0} in TcpReadCallback EndReceive()", e.Message);
			}

			if (bytesRead > 0)
			{
				// Parse
				m_MessageParser.ParseMessage(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

				// Clear
				state.buffer = new byte[ServerDefs.BUFF_SIZE];

				// Start a new callback
				state.tcpSocket.BeginReceive(state.buffer, 0, ServerDefs.BUFF_SIZE,
					0, new AsyncCallback(TcpReadCallback), state
				);
			}
			else
			{
				// Shut down
				Logger.Log("Removing client and closing sockets");

				m_ServerManager.RemoveClient(tcp_socket);

				// Re-allocate next time we get a client
				if (m_ServerManager.NumClients() == 0)
				{
					m_GameSimulation.ClearGameData();
				}

				Logger.Log(string.Format("Number of clients: {0}", m_ServerManager.NumClients()), Logger.LogPrio.Warning);
			}
		}

		public void UdpInitialReadCallback(IAsyncResult ar)
		{
			Logger.Log("Got clients first udp message..... attemping to bind sockets");

			UdpStateObject listenstate = (UdpStateObject)(ar.AsyncState);
			UdpClient udplistener = listenstate.udpSocket;
			IPEndPoint remoteEndpoint = listenstate.endPoint;

			Byte[] receiveBytes = udplistener.EndReceive(ar, ref remoteEndpoint);
			string receiveString = Encoding.ASCII.GetString(receiveBytes);

			string[] spl_str = receiveString.Split(':');
			Int32 id = Convert.ToInt32(spl_str[0]);

			if(m_ServerManager.ClientExists(id))
			{
				if (m_ServerManager.ConnectToRemoteEndPoint(id, remoteEndpoint))
				{
					// Send Back to sender only to establish udp connection
					m_ServerManager.SendUdp(id, receiveString);
					udplistener.BeginReceive(new AsyncCallback(UdpReadCallback), listenstate);
				}
				else
				{
					Logger.Log("Problem connecting to remote end point" + remoteEndpoint.ToString(), Logger.LogPrio.Error);
				}
			}
			else
			{
				// This client didn't exist
				Console.WriteLine(string.Format("Error: Client with id {0} does not exist", id));
				udplistener.Close();
			}
		}

		public void UdpReadCallback(IAsyncResult ar)
		{
			UdpStateObject listenstate = (UdpStateObject)(ar.AsyncState);
			UdpClient udplistener = listenstate.udpSocket;
			IPEndPoint remoteEndpoint = listenstate.endPoint;

			Byte[] receiveBytes = udplistener.EndReceive(ar, ref remoteEndpoint);

			m_MessageParser.ParseMessage(Encoding.ASCII.GetString(receiveBytes));

			// Loop around
			udplistener.BeginReceive(new AsyncCallback(UdpReadCallback), listenstate);
		}
	}
}
