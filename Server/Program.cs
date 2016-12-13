using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;     
using System.Net.Sockets;
using System.Threading;

using System.Data;

using Microsoft.Xna.Framework;

// This
using Server.Server;
using Server.GameSpecific;

namespace Server
{


	#region [   data objects   ]

	[Serializable]
	public class basepacket
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class regPacket : basepacket
	{
		public int clientId;
		public int udpPort;

		public regPacket()
		{
		}

		public regPacket(string name, int clientId, int udpPort)
		{
			Name = name;
			this.clientId = clientId;
			this.udpPort = udpPort;
		}
	}
	#endregion

	class Program
	{
		// Move this
		const int iterations = 3;
		const int UP = 0;
		const int DOWN = 1;
		const int LEFT = 2;
		const int RIGHT = 3;

		static List<GameObject> GameObjects = new List<GameObject>();

		enum PacketID
		{
			Register,
			CreateObject,
			UpdateObject,
		}

		public class AsynchSocketListener
		{
			// Thread signal.
			public static ManualResetEvent allDone = new ManualResetEvent(false);

			static Dictionary<Int32, GameClient> m_Clients = new Dictionary<int, GameClient>();
			public AsynchSocketListener()
			{
			}

			static void ClearGameData()
			{
				GameObjects.Clear();
			}

			static void SetLevelData(int levelNumber)
			{

				for (int i = 1; i < 12; ++i)
				{
					GameObjects.Add(new GameObject(new Vector2(i * 64, 450), GameObjectType.Wall, GameObjects.Count));
				}

				GameObjects.Add(new GameObject(new Vector2(7 * 64, 350), GameObjectType.Wall, GameObjects.Count));
			}


			public static void StartListeningForNewClients()
			{
				IPEndPoint addr = new IPEndPoint(IPAddress.Any, ServerDefs.TCP_LISTEN_PORT);
				Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				Console.WriteLine("Server started.... waiting for connections from new clients");

				try
				{			
					listener.Bind(addr);
					listener.Listen(100);

					while (true)
					{
						allDone.Reset();
						listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
						allDone.WaitOne();
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(string.Format("Exception handled : {0}", e.Message));
				}
			}

			public static void AcceptCallback(IAsyncResult ar)
			{
				allDone.Set();

				// ---- Set up tcp socket ----
				Socket listener = (Socket)ar.AsyncState;
				Socket localTcp = listener.EndAccept(ar);
				//Console.WriteLine("Client Connected to TCP socket");

				// ---- Generate a state object to pass around Async calls ----
				TcpStateObject tcpStateObj = new TcpStateObject();
				tcpStateObj.tcpSocket = localTcp;

				// Create Level data if no clients (THIS WILL ALL BE MOVED LATER)
				if (m_Clients.Count == 0)
					SetLevelData(0);
				
				// ---- Generate simple client id hash ----
				int hash = m_Clients.Count;

				// ---- Add client to hash table 
				m_Clients.Add(hash, new GameClient(tcpStateObj.tcpSocket, null,null));

				// ---- Gen a Udp state object ----
				IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);		// Generate free port to listen on
				UdpClient localUdp = new UdpClient(ep);                     // Create 

				UdpStateObject udpStateObj = new UdpStateObject();
				udpStateObj.endPoint = ep; 
				udpStateObj.udpSocket = localUdp;

				// Resolve system generated port of local udp socket
				int port = ((IPEndPoint)localUdp.Client.LocalEndPoint).Port;
				m_Clients[hash].udpLocalPort = port;

				// **Important -- SEND Register Function and give them their id which they must store, and use in all packets
				//				  so we know who they are, also send them local udp port that we are listening for them on.

				SendTcp(tcpStateObj.tcpSocket, string.Format("reg:{0}:{1}:!", hash, port));

				// We want to add them to the level and send them the level
				m_Clients[hash].playerObjectHandle = GameObjects.Count;

				GameObject newPlayer = new GameObject(GameObjectType.Player, GameObjects.Count);
				newPlayer.isClientPlayer = 1;
				GameObjects.Add(newPlayer);

				// TODO : Need to associate this player object with the client and rmeove it too
				
				// --- Current clients need to add this new player game object
				SendAllTcpExcept(string.Format("mapdata:{0},{1},{2},{3},{4},:!", (int)newPlayer.object_id, newPlayer.unique_id, (int)newPlayer.Position.X, (int)newPlayer.Position.Y, 0), hash);

				// Send new client all of the game objects
				string LevelPacket = "mapdata:";
				foreach (GameObject go in GameObjects)
				{
					LevelPacket += string.Format("{0},{1},{2},{3},{4},:",
						(int)go.object_id,
						go.unique_id,
						(int)go.Position.X,
						(int)go.Position.Y,
						go.isClientPlayer);
				}

				LevelPacket += "!";

				SendTcp(tcpStateObj.tcpSocket, LevelPacket);

				// Set this back for the next new client
				newPlayer.isClientPlayer = 0;

				// ---- Begin Async Tcp receive for this client
				tcpStateObj.tcpSocket.BeginReceive(
					tcpStateObj.buffer,						// Buffer
					0,                                      // Offset
					ServerDefs.BUFF_SIZE,					// Size
					0,										// Flags
					new AsyncCallback(TcpReadCallback),		// Callback
					tcpStateObj								// Object
				);

				//Console.WriteLine("Listening for UDP messages");

				// ----  The client knows this local udp port, so if/when we get a message from them on this, we can add their end
				//		 point to the client hash table and connect a local udp Socket to that end point which is also stored and
				//		 completes the data structure for this client
				localUdp.BeginReceive(new AsyncCallback(UdpInitialReadCallback), udpStateObj);

				Console.WriteLine("New client connected to server");
				Console.WriteLine(string.Format("Number of clients connected {0}.\nNew client TCP Endpoint {1}.\nWaiting for udp connection on port {2}",
					m_Clients.Count, localTcp.RemoteEndPoint, port));
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

				Console.WriteLine("Got TCP Msg");

				if (bytesRead > 0)
				{
					// Parse
					state.strBldr.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

					// TODO: Parse, send all for now
					//SendAllTcp(state.strBldr.ToString());
					
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
					Console.WriteLine("Removing clients and closing sockets");
					Console.WriteLine(string.Format("Number of clients: {0}", m_Clients.Count));

					tcp_socket.Close();

					foreach (KeyValuePair<int, GameClient> kvp in m_Clients)
					{
						GameClient client = kvp.Value;

						// Set to empty
						GameObjects[client.playerObjectHandle].object_id = GameObjectType.Empty;

						if (client.tcpSocket == tcp_socket)
						{
							if (client.udpSocket != null && client.udpSocket.Client.Connected)
							{
								client.udpSocket.Close();
							}

							m_Clients.Remove(kvp.Key);
							break;
						}
					}

					// Re-allocate next time we get a client
					if (m_Clients.Count == 0)
					{
						ClearGameData();
					}

					Console.WriteLine(string.Format("Number of clients now: {0}", m_Clients.Count));
				}
			}

			public static void UdpInitialReadCallback(IAsyncResult ar)
			{
				Console.WriteLine("Got clients first udp message..... attemping to bind sockets");

				UdpStateObject listenstate = (UdpStateObject)(ar.AsyncState);
				UdpClient udplistener = listenstate.udpSocket;
				IPEndPoint remoteEndpoint = listenstate.endPoint;

				Byte[] receiveBytes = udplistener.EndReceive(ar, ref remoteEndpoint);
				string receiveString = Encoding.ASCII.GetString(receiveBytes);

				string[] spl_str = receiveString.Split(':');
				Int32 id = Convert.ToInt32(spl_str[0]);

				if (m_Clients.ContainsKey(id))
				{
					if (m_Clients[id].udpRemoteEndpoint == null)
					{
						m_Clients[id].udpRemoteEndpoint = remoteEndpoint;

						if (m_Clients[id].udpSocket == null)
						{
							m_Clients[id].udpSocket = new UdpClient();
							m_Clients[id].udpSocket.Connect(m_Clients[id].udpRemoteEndpoint);

							Console.WriteLine(string.Format("Set up Udp connect for client with id:{0}. Clients port {1}", id , m_Clients[id].udpRemoteEndpoint.Port));

							// Send Back to sender only
							SendUdp(m_Clients[id].udpSocket, receiveString);

							// Loop around with new Read callback
							udplistener.BeginReceive(new AsyncCallback(UdpReadCallback), listenstate);
						}
					}
					else
					{
						Console.WriteLine(string.Format("Warning: Client with id {0} has tried to register twice", id));
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
				Console.WriteLine("Got Udp");

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

					if (type == "input")
					{
						int key =       Convert.ToInt32(spl_str[1]);
						int client_id = Convert.ToInt32(spl_str[2]);

						// Get this from handle
						int playerHandle = m_Clients[client_id].playerObjectHandle;

						Vector2 velocity = Vector2.Zero;
						GameObject player = GameObjects[playerHandle];
						Vector2 m_Position = player.Position;

						// Right 
						if (key == 3)
						{
							velocity = new Vector2(6.0f, 0.0f);
						}
						// Left
						else if(key == 0)
						{
							velocity = new Vector2(-6.0f, 0.0f);
						}
						// Up
						else if (key == 22)
						{
							velocity = new Vector2(0.0f, -6.0f);
						}
						// Down
						else if (key == 18)
						{
							velocity = new Vector2(0.0f, 6.0f);
						}

						// TODO : Resolve collision
						bool contactLeft = false, contactRight = false, contactYbottom = false, contactYtop = false;
						Vector2 predicted_speed = velocity * (1/60.0f);	// TODO : Get player to pass delta time
						float projectedMoveX, projectedMoveY, originalMoveX, originalMoveY;
						originalMoveX = predicted_speed.X;
						originalMoveY = predicted_speed.Y;

						foreach (GameObject go in GameObjects)
						{
							if (go.object_id == GameObjectType.Wall)
							{
								for (int dir = 0; dir < 4; dir++)
								{
									if (dir == UP && predicted_speed.Y > 0) continue;
									if (dir == DOWN && predicted_speed.Y < 0) continue;
									if (dir == LEFT && predicted_speed.X > 0) continue;
									if (dir == RIGHT && predicted_speed.X < 0) continue;

									projectedMoveX = (dir >= LEFT ? predicted_speed.X : 0);
									projectedMoveY = (dir < LEFT ? predicted_speed.Y : 0);

									while ((go.bounds.Contains(player.points[dir * 2].X + (int)m_Position.X + (int)projectedMoveX,
										player.points[dir * 2].Y + (int)m_Position.Y + (int)projectedMoveY)
										||
										go.bounds.Contains(player.points[dir * 2 + 1].X + (int)m_Position.X + (int)projectedMoveX,
											player.points[dir * 2 + 1].Y + (int)m_Position.Y + (int)projectedMoveY)))
									{
										if (dir == UP)
											projectedMoveY++;
										if (dir == DOWN)
											projectedMoveY--;
										if (dir == LEFT)
											projectedMoveX++;
										if (dir == RIGHT)
											projectedMoveX--;
									}

									if (dir >= LEFT && dir <= RIGHT)
										predicted_speed.X = projectedMoveX;
									if (dir >= UP && dir <= DOWN)
										predicted_speed.Y = projectedMoveY;
								}

								// Resolve contact
								if (predicted_speed.Y > originalMoveY && originalMoveY < 0)
								{
									contactYtop = true;
								}

								if (predicted_speed.Y < originalMoveY && originalMoveY > 0)
								{
									contactYbottom = true;
								}

								if (predicted_speed.X - originalMoveX < -0.01f)
								{
									contactRight = true;
								}

								if (predicted_speed.X - originalMoveX > 0.01f)
								{
									contactLeft = true;
								}

								// Resolve collision form contact
								if (contactYbottom || contactYtop)
								{
									//m_Position.Y += predicted_speed.Y;
									velocity.Y = 0;	// Would need to send this back to player
								}

								if (contactLeft || contactRight)
								{
									//m_Position.X += predicted_speed.X;
									velocity.X = 0;
								}
							}
						}

						//m_Position += velocity;
						//GameObjects[playerHandle].Position = new Vector2(((float)(int)m_Position.X), ((float)(int)m_Position.Y));
						GameObjects[playerHandle].Position += velocity;

						// Send updated object
						SendAllUdp(string.Format("objupd:{0}:{1}:{2}:",
							playerHandle,
							(int)GameObjects[playerHandle].Position.X,
							(int)GameObjects[playerHandle].Position.Y
						));
					}
				}

				// Echo for now
				//SendAllUdp(receiveString);

				// Loop around
				udplistener.BeginReceive(new AsyncCallback(UdpReadCallback), listenstate);
			}

			public static void UdpSendCallback(IAsyncResult ar)
			{
				UdpClient uc = (UdpClient)ar.AsyncState;
				Console.WriteLine("UDP number of bytes sent: {0}", uc.EndSend(ar));
			}

			public static void TcpSendCallBack(IAsyncResult ar)
			{
				try
				{
					Socket handler = (Socket)ar.AsyncState;
					int bytesSent = handler.EndSend(ar);
				}
				catch (Exception e)
				{
					Console.WriteLine(string.Format("Handled Exception : ", e.ToString()));
				}
			}

			static void SendUdp(UdpClient client, string message)
			{
				Byte[] sendBytes = Encoding.ASCII.GetBytes(message);

				// send the message
				// the destination is defined by the call to .Connect()
				client.BeginSend(sendBytes, sendBytes.Length,
							new AsyncCallback(UdpSendCallback), client);
			}

			public static void SendAllUdp(String data)
			{
				foreach (KeyValuePair<int, GameClient> client in m_Clients)
				{
					try
					{
						SendUdp(client.Value.udpSocket, data);
					}
					catch (Exception e)
					{
						Console.WriteLine(string.Format("Error trying to send udp message: {0}", e.Message));
					}
				}
			}

			public static void SendTcp(Socket handler, String data)
			{
				byte[] packet = Encoding.ASCII.GetBytes(data);

				handler.BeginSend(packet, 0, packet.Length, 0,
					new AsyncCallback(TcpSendCallBack), handler);
			}

			public static void SendTcp_Bin(Socket handler, byte[] packet)
			{
				handler.BeginSend(packet, 0, packet.Length, 0,
					new AsyncCallback(TcpSendCallBack), handler);
			}

			public static void SendAllTcp(String data)
			{
				foreach (KeyValuePair<int, GameClient> kvp in m_Clients)
				{
					SendTcp(kvp.Value.tcpSocket, data);
				}
			}

			public static void SendAllTcpExcept(String data, int ignore)
			{
				foreach (KeyValuePair<int, GameClient> kvp in m_Clients)
				{
					if(kvp.Key != ignore)
						SendTcp(kvp.Value.tcpSocket, data);
				}
			}
		}

		static void Main(string[] args)
		{
			//string json = "{ \"name\" : \"alex\", \"age\" : 29 } ";

			// Pack into JSON like this to send out
			regPacket rp = new regPacket("reg", 0, 2456);
			string jsonS = fastJSON.JSON.ToJSON(rp);

			// Do this to parse back
			Dictionary<string, object> JDATA = (Dictionary<string, object>)fastJSON.JSON.Parse(jsonS);

			if (JDATA.ContainsKey("Name"))
			{
				if (JDATA["Name"] == "reg")
				{
					// Could deal with individual members here or cast to packet
				}
			}

			AsynchSocketListener.StartListeningForNewClients();
			Console.ReadLine();	
		}
	}
}
