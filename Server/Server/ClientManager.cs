using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using Server.Utils;
using Server.GameSpecific;

namespace Server.Server
{
    public class ServerManager
    {
        private static ServerManager _instance;
        public static ServerManager instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServerManager();
                }

                return _instance;
            }

            private set {; }
        }

        // TODO : We need sets of these for each game
        private Dictionary<Int32, GameClient> m_Clients = new Dictionary<int, GameClient>();

        // TODO : This will be a database
        private List<string> UserAccounts = new List<string>();

        public ServerManager()
        {
        }

        #region AccountManagement
        public string AddNewUserToDatabase(string userName, int clientId)
        {
            if (UserExistsInDatabase(userName))
            {
                string err = string.Format("Error: User {0} already exists", userName);
                Logger.Log(err, Logger.LogPrio.Error);
                return err;
            }

            if (!ClientExists(clientId))
            {
                string err = string.Format("Error unknown client id: {0}", clientId);
                Logger.Log(err, Logger.LogPrio.Error);
                return err;
            }

            UserAccounts.Add(userName);

            string s = string.Format("User account created for {0}", userName);
            return s;
        }

        public string Login(string userName, int clientId, out bool success)
        {
            if (!UserExistsInDatabase(userName))
            {
                string err = string.Format("Error: user {0} does not exist", userName);
                Logger.Log(err, Logger.LogPrio.Error);
                success = false;
                return err;
            }

            if (!ClientExists(clientId))
            {
                string err = string.Format("Tried to create account with unknown client id: {0}", clientId);
                Logger.Log(err, Logger.LogPrio.Error);
                success = false;
                return err;
            }


            m_Clients[clientId].loggedIn = true;

            string s = string.Format("Client {0} is now logged in and ready to play", userName);
            Logger.Log(s);
            success = true;
            return s;
        }

        private bool UserExistsInDatabase(string userName)
        {
            foreach (string name in UserAccounts)
            {
                if (name == userName)
                    return true;
            }

            return false;
        }
        #endregion

        #region Client Management
        public int AddNewClient(Socket tcpSocket)
		{
            int hash = 0;
            lock (m_Clients)
            {
                hash = m_Clients.Count;
                m_Clients.Add(hash, new GameClient(tcpSocket, null, null));
            }
			return hash;
		}

		public int NumClients()
		{
			return m_Clients.Count;
		}

		public bool ClientExists(int id)
		{
			return m_Clients.ContainsKey(id);
		}

		public GameClient GetClient(int id)
		{
			return m_Clients[id];
		}

        public Dictionary<int, GameClient> GetClients()
        {
            return m_Clients;
        }

		public void SetLocalUdpPort(int clientId, int port)
		{
			m_Clients[clientId].udpLocalPort = port;
		}

		public int GetPlayerHandle(int clientId)
		{
			return m_Clients[clientId].playerObjectHandle;
		}

		public void SetPlayerHandle(int clientId, int handle)
		{
			m_Clients[clientId].playerObjectHandle = handle;
		}

		public bool ConnectToRemoteEndPoint(int clientId, IPEndPoint remEndPt)
		{
            lock(m_Clients)
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
		}

		public void RemoveClient(Socket tcpMatch)
		{
            lock(m_Clients)
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
		}
		#endregion

		#region Sending Data
		private void UdpSendCallback(IAsyncResult ar)
		{
            try
            {
                UdpClient uc = (UdpClient)ar.AsyncState;
                uc.EndSend(ar); // returns int of bytes sent
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("Handled Exception : {0} in UdpSendCallback", e.ToString()), Logger.LogPrio.Error);
            }
        }

		private void TcpSendCallBack(IAsyncResult ar)
		{
			try
			{
				Socket handler = (Socket)ar.AsyncState;
				int bytesSent = handler.EndSend(ar);
			}
			catch (Exception e)
			{
				Logger.Log(string.Format("Handled Exception :{0} in TcpSendCallback ", e.ToString()), Logger.LogPrio.Error);
			}
		}


		void SendUdp(UdpClient client, string message)
		{
            try
            {
                Byte[] sendBytes = Encoding.ASCII.GetBytes(message);
                client.BeginSend(sendBytes, sendBytes.Length,
                    new AsyncCallback(UdpSendCallback), client);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("Handled Exception : {0} in SendUdp(UdpClient, string)", e.ToString()), Logger.LogPrio.Error);
            }
        }

        public void SendUdp(int clientId, string msg)
		{
			SendUdp(m_Clients[clientId].udpSocket, msg);
		}

        public void SendAllUdp(String data)
		{
            try
            {
                foreach (KeyValuePair<int, GameClient> client in m_Clients)
                {
                    SendUdp(client.Value.udpSocket, data);
                }
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("Handled Exception : {0} in SendAllUdp", e.ToString()), Logger.LogPrio.Error);
            }
        }

		public void SendTcp(Socket handler, String data)
		{
			byte[] packet = Encoding.ASCII.GetBytes(data);

			handler.BeginSend(packet, 0, packet.Length, 0,
				new AsyncCallback(TcpSendCallBack), handler);
		}

		public void SendAllTcp(String data)
		{
			foreach (KeyValuePair<int, GameClient> kvp in m_Clients)
			{
				SendTcp(kvp.Value.tcpSocket, data);
			}
		}

		public void SendAllTcpExcept(String data, int ignore, bool checkLoggedIn = false)
		{
			foreach (KeyValuePair<int, GameClient> kvp in m_Clients)
			{
                if (kvp.Key != ignore)
                {
                     if((checkLoggedIn && kvp.Value.loggedIn) || !checkLoggedIn)
                        SendTcp(kvp.Value.tcpSocket, data);
                }
			}
		}
		#endregion
	}
}
