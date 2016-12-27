using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public static ManualResetEvent allDone = new ManualResetEvent(false);

		#region Member Functions

		public AsynchSocketListener()
		{
		}

		public static void StartListeningForNewClients()
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

		private static void AcceptCallback(IAsyncResult ar)
		{
			allDone.Set();

			// ---- Set up tcp socket ----
			Socket listener = (Socket)ar.AsyncState;
			Socket localTcp = listener.EndAccept(ar);

			// ---- Generate a state object to pass around Async calls ----
			TcpStateObject tcpStateObj = new TcpStateObject();
			tcpStateObj.tcpSocket = localTcp;


			//****** TODO MOVE THIS TO WHEN WE GET A NEW GAME REQUEST *****
			// Create Level data if no clients (THIS WILL ALL BE MOVED LATER)
			if(ServerManager.NumClients() == 0 )
			{
				GameSimulation.LoadLevel(0);
			}

			// ---- Add a new client with this Tcp socket and get the hash ----
			int hash = ServerManager.AddNewClient(tcpStateObj.tcpSocket);

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
			ServerManager.SetLocalUdpPort(hash, port);

			// **Important -- SEND Register Function and give them their id which they must store, and use in all packets
			//				  so we know who they are, also send them local udp port that we are listening for them on.

			ServerManager.SendTcp(tcpStateObj.tcpSocket, string.Format("reg:{0}:{1}:!", hash, port));

			// We want to add them to the level and send them the level
			ServerManager.SetPlayerHandle(hash, GameSimulation.NumObjects());

			GameObject newPlayer = new GameObject(GameObjectType.Player, GameSimulation.NumObjects());
			newPlayer.isClientPlayer = 1;
			GameSimulation.AddGameObject(newPlayer);

			// TODO : Need to associate this player object with the client and rmeove it too

			// --- Current clients need to add this new player game object
			ServerManager.SendAllTcpExcept(string.Format("mapdata:{0},{1},{2},{3},{4},:!", (int)newPlayer.object_id, newPlayer.unique_id, (int)newPlayer.Position.X, (int)newPlayer.Position.Y, 0), hash);

			// Send new client all of the game objects
			string LevelPacket = "mapdata:";
			foreach (GameObject go in GameSimulation.GetObjects())
			{
				LevelPacket += string.Format("{0},{1},{2},{3},{4},:",
					(int)go.object_id,
					go.unique_id,
					(int)go.Position.X,
					(int)go.Position.Y,
					go.isClientPlayer);
			}

			LevelPacket += "!";

			ServerManager.SendTcp(tcpStateObj.tcpSocket, LevelPacket);

			// Set this back for the next new client
			newPlayer.isClientPlayer = 0;

			// *** TODO SET THIS BACK, or test if it needs to

			// ---- Begin Async Tcp receive for this client
			tcpStateObj.tcpSocket.BeginReceive(
				tcpStateObj.buffer,                     // Buffer
				0,                                      // Offset
				ServerDefs.BUFF_SIZE,                   // Size
				0,                                      // Flags
				new AsyncCallback(TcpReadCallback),     // Callback
				tcpStateObj                             // Object
			);

			// ----  The client knows this local udp port, so if/when we get a message from them on this, we can add their end
			//		 point to the client hash table and connect a local udp Socket to that end point which is also stored and
			//		 completes the data structure for this client
			localUdp.BeginReceive(new AsyncCallback(UdpInitialReadCallback), udpStateObj);

			Logger.Log("New client connected to server");
			Logger.Log(string.Format("Number of clients connected {0}.\nNew client TCP Endpoint {1}.\nWaiting for udp connection on port {2}",
				ServerManager.NumClients(), localTcp.RemoteEndPoint, port));
		}

		public static void TcpReadCallback(IAsyncResult ar)
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
				Console.WriteLine("Exception handled :  {0}", e.Message);
			}

			if (bytesRead > 0)
			{
				// **** TODO Pass this over to a parsing system

				// Parse
				state.strBldr.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

				// Clear
				state.buffer = new byte[ServerDefs.BUFF_SIZE];
				state.strBldr.Clear();

				// Start a new callback?
				state.tcpSocket.BeginReceive(
					state.buffer,
					0,
					ServerDefs.BUFF_SIZE,
					0,
					new AsyncCallback(TcpReadCallback),
					state
				);
			}
			else
			{
				// Shut down
				Logger.Log("Removing client and closing sockets");

				ServerManager.RemoveClient(tcp_socket);

				// Re-allocate next time we get a client
				if (ServerManager.NumClients() == 0)
				{
					GameSimulation.ClearGameData();
				}

				Logger.Log(string.Format("Number of clients: {0}", ServerManager.NumClients()), Logger.LogPrio.Warning);
			}
		}

		public static void UdpInitialReadCallback(IAsyncResult ar)
		{
			Logger.Log("Got clients first udp message..... attemping to bind sockets");

			UdpStateObject listenstate = (UdpStateObject)(ar.AsyncState);
			UdpClient udplistener = listenstate.udpSocket;
			IPEndPoint remoteEndpoint = listenstate.endPoint;

			Byte[] receiveBytes = udplistener.EndReceive(ar, ref remoteEndpoint);
			string receiveString = Encoding.ASCII.GetString(receiveBytes);

			string[] spl_str = receiveString.Split(':');
			Int32 id = Convert.ToInt32(spl_str[0]);

			if(ServerManager.ClientExists(id))
			{
				if (ServerManager.ConnectToRemoteEndPoint(id, remoteEndpoint))
				{
					// Send Back to sender only to establish udp connection
					ServerManager.SendUdp(id, receiveString);

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

		public static void UdpReadCallback(IAsyncResult ar)
		{
			UdpStateObject listenstate = (UdpStateObject)(ar.AsyncState);
			UdpClient udplistener = listenstate.udpSocket;
			IPEndPoint remoteEndpoint = listenstate.endPoint;

			Byte[] receiveBytes = udplistener.EndReceive(ar, ref remoteEndpoint);
			string receiveString = Encoding.ASCII.GetString(receiveBytes);

			// Parse here for now, hardcoded one off thing for now
			string[] spl_str = receiveString.Split(':');
			if (spl_str.Length >= 3)
			{
				string type = spl_str[0];

				// Dont want to do this here
				// ****** TODO : Need a hash here
				if (type == "input")
				{
					int key = Convert.ToInt32(spl_str[1]);
					int client_id = Convert.ToInt32(spl_str[2]);

					// Get this from handle
					//int playerHandle = m_Clients[client_id].playerObjectHandle;
					int playerHandle = ServerManager.GetPlayerHandle(client_id);

					GameSimulation.InputUpdate(playerHandle, key);
				}
			}

			// Loop around
			udplistener.BeginReceive(new AsyncCallback(UdpReadCallback), listenstate);
		}

		#endregion
	}
}
