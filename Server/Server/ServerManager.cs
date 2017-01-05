using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Data.SQLite;
using System.IO;
using Microsoft.Xna.Framework;

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

        private SQLiteConnection m_Database = null;

        public ServerManager()
        {
            this.CreateOrOpenDatabase();
        }

        #region DatabaseManagement
        private void CreateOrOpenDatabase()
        {
            if (!File.Exists("PlayerDB.sqlite"))
            {
                Console.WriteLine("Creating new database");

                SQLiteConnection.CreateFile("PlayerDB.sqlite");

                m_Database = new SQLiteConnection("Data Source=PlayerDB.sqlite;Version=3;");
                m_Database.Open();

                // Create the table that will work with on the server
                string db_players = "CREATE TABLE t_Players (name VARCHAR(20), password VARCHAR(10), exp INT)";
                SQLiteCommand sqlCmd = new SQLiteCommand(db_players, m_Database);
                sqlCmd.ExecuteNonQuery();
            }
            else
            {
                Console.WriteLine("Opening existing database");

                // Just open it if it's already created
                m_Database = new SQLiteConnection("Data Source=PlayerDB.sqlite;Version=3;");
                m_Database.Open();
            }

            // TODO : Test, can remove this
            //PrintLeaderBoard();
        }

        private bool CheckClientInDatabase(string name)
        {
            string sqlQueery = "SELECT * FROM [t_Players] order by [exp] desc";
            SQLiteCommand qryCmd = new SQLiteCommand(sqlQueery, m_Database);

            SQLiteDataReader reader = qryCmd.ExecuteReader();
            while (reader.Read())
            {
                if (name == (string)reader["name"])
                    return true;
            }

            return false;
        }

        private bool CheckClientAndPasswordMatch(string name, string pw) // OR LOGIN
        {
            string sqlQueery = "SELECT * FROM [t_Players] order by [exp] desc";
            SQLiteCommand qryCmd = new SQLiteCommand(sqlQueery, m_Database);

            SQLiteDataReader reader = qryCmd.ExecuteReader();
            while (reader.Read())
            {
                if (name == (string)reader["name"] && pw == (string)reader["password"])
                    return true;
            }

            return false;
        }

        public string AddNewClientToDatabase(int clientId, string userName, string pw)
        {
            if (CheckClientInDatabase(userName))
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

            string insert = string.Format("INSERT into [t_Players] (name, password, exp) values ('{0}', '{1}', 0)", userName, pw);
            SQLiteCommand sqlInsCmd = new SQLiteCommand(insert, m_Database);
            sqlInsCmd.ExecuteNonQuery();

            string s = string.Format("User account created for {0}", userName);
            return s;
        }

        public string Login(string userName, string password, int clientId, out bool success)
        {
            if (!CheckClientAndPasswordMatch(userName, password))
            {
                string err = string.Format("Error: user {0} does not exist", userName);
                Logger.Log(err, Logger.LogPrio.Error);
                success = false;
                return err;
            }

            m_Clients[clientId].loggedIn = true;
			m_Clients[clientId].userName = userName;

            string s = string.Format("Client {0} is now logged in and ready to play", userName);
            Logger.Log(s);
            success = true;
            return s;
        }

        public void UpdateClientExpDB(string name, int amount)
        {
            string update = string.Format("UPDATE [t_Players] set [exp] = [exp] + '{0}' where [name] = '{1}'", amount, name);
            SQLiteCommand cmd = new SQLiteCommand(update, m_Database);
            cmd.ExecuteNonQuery();
        }

		public int GetClientExp(string name)
		{
			string sqlQueery = "select * from t_Players order by exp desc";
			SQLiteCommand qryCmd = new SQLiteCommand(sqlQueery, m_Database);

			SQLiteDataReader reader = qryCmd.ExecuteReader();
			while (reader.Read())
			{
				if ((string)reader["name"] == name)
				{
					return (int)reader["exp"];
				}
			}

			return 0;
		}

		public void PrintLeaderBoard()
        {
            string sqlQueery = "select * from t_Players order by exp desc";
            SQLiteCommand qryCmd = new SQLiteCommand(sqlQueery, m_Database);

            SQLiteDataReader reader = qryCmd.ExecuteReader();
            while (reader.Read())
                Console.WriteLine("Name: " + reader["name"] + "\tExp: " + reader["exp"]);
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
			if(m_Clients.ContainsKey(id))
				return m_Clients[id];
			return null;
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

		public void ResetClientHandles()
		{
			foreach (GameClient c in m_Clients.Values)
			{
				c.playerObjectHandle = -1;
				c.inGame = false;
			}
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

                    if (client.tcpSocket == tcpMatch)
                    {
						// Need to set the objetc that was associated with this player to deactivate, but can still be re-used by new player
						if (client.playerObjectHandle < GameSimulation.instance.NumObjects())
						{
							GameObject quitPlayer = GameSimulation.instance.GetObject(client.playerObjectHandle);
							if (quitPlayer != null)
							{
								quitPlayer.Active = false;
								quitPlayer.Position = Vector2.Zero;
								quitPlayer.Velocity = Vector2.Zero;
							}
						}

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

		private void SendUdp(UdpClient client, string message)
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
			if(m_Clients.ContainsKey(clientId))
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
