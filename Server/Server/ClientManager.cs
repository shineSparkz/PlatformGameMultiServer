using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using Server.Utils;

namespace Server.Server
{
	class ServerManager
	{
		static Dictionary<Int32, GameClient> m_Clients = new Dictionary<int, GameClient>();

		#region Client Management
		public static int AddNewClient(Socket tcpSocket)
		{
			int hash = m_Clients.Count;
			m_Clients.Add(hash, new GameClient(tcpSocket, null, null));
			return hash;
		}

		public static int NumClients()
		{
			return m_Clients.Count;
		}

		public static bool ClientExists(int id)
		{
			return m_Clients.ContainsKey(id);
		}

		public static GameClient GetClient(int id)
		{
			return m_Clients[id];
		}

		public static void SetLocalUdpPort(int clientId, int port)
		{
			m_Clients[clientId].udpLocalPort = port;
		}

		public static int GetPlayerHandle(int clientId)
		{
			return m_Clients[clientId].playerObjectHandle;
		}

		public static void SetPlayerHandle(int clientId, int handle)
		{
			m_Clients[clientId].playerObjectHandle = handle;
		}

		public static bool ConnectToRemoteEndPoint(int clientId, IPEndPoint remEndPt)
		{
			if (m_Clients[clientId].udpRemoteEndpoint == null)
			{
				m_Clients[clientId].udpRemoteEndpoint = remEndPt;

				if (m_Clients[clientId].udpSocket == null)
				{
					m_Clients[clientId].udpSocket = new UdpClient();
					m_Clients[clientId].udpSocket.Connect(m_Clients[clientId].udpRemoteEndpoint);

					Logger.Log(string.Format("Set up Udp connect for client with id:{0}. Clients port {1}", clientId, m_Clients[clientId].udpRemoteEndpoint.Port));
					return true;
				}
				else
				{
					// Log
					return false;
				}
			}
			else
			{
				// Log
				return false;
			}
		}

		public static void RemoveClient(Socket tcpMatch)
		{
			foreach (KeyValuePair<int, GameClient> kvp in m_Clients)
			{
				GameClient client = kvp.Value;

				// Set to empty // TODO ::::::::
				//GameObjects[client.playerObjectHandle].object_id = GameObjectType.Empty;

				if (client.tcpSocket == tcpMatch)
				{
					if (client.udpSocket != null && client.udpSocket.Client.Connected)
					{
						client.udpSocket.Close();
						client.tcpSocket.Close();
					}

					m_Clients.Remove(kvp.Key);
					break;
				}
			}
		}
		#endregion

		#region Sending Data
		private static void UdpSendCallback(IAsyncResult ar)
		{
			UdpClient uc = (UdpClient)ar.AsyncState;
			uc.EndSend(ar); // returns int of bytes sent
		}

		private static void TcpSendCallBack(IAsyncResult ar)
		{
			try
			{
				Socket handler = (Socket)ar.AsyncState;
				int bytesSent = handler.EndSend(ar);
			}
			catch (Exception e)
			{
				Logger.Log(string.Format("Handled Exception : ", e.ToString()), Logger.LogPrio.Error);
			}
		}


		public static void SendUdp(int clientId, string msg)
		{
			SendUdp(m_Clients[clientId].udpSocket, msg);
		}

		static void SendUdp(UdpClient client, string message)
		{
			Byte[] sendBytes = Encoding.ASCII.GetBytes(message);
			client.BeginSend(sendBytes, sendBytes.Length,
				new AsyncCallback(UdpSendCallback), client);
		}

		public static void SendAllUdp(String data)
		{
			foreach (KeyValuePair<int, GameClient> client in m_Clients)
			{
				SendUdp(client.Value.udpSocket, data);
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
				if (kvp.Key != ignore)
					SendTcp(kvp.Value.tcpSocket, data);
			}
		}
		#endregion
	}
}
