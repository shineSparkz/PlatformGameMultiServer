using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Utils;
using Server.GameSpecific;
using Server.GameSpecific.GameObjects;

using Microsoft.Xna.Framework;

namespace Server.Server
{
	public class MessageParser
	{
		private Dictionary<PacketDefs.ID, Func<Dictionary<string, object>, bool>> MessageFunctions;
		private GameSimulation m_GameSimulation = null;
		private ServerManager m_ServerManager = null;

		public MessageParser(GameSimulation gs, ServerManager sm)
		{
			m_GameSimulation = gs;
			m_ServerManager = sm;

			MessageFunctions = new Dictionary<PacketDefs.ID, Func<Dictionary<string, object>, bool>>();

			MessageFunctions[PacketDefs.ID.IN_UDP_Input] = InputMsg;

            MessageFunctions[PacketDefs.ID.IN_TCP_CreateAccount] = CreateAccountMsg;
            MessageFunctions[PacketDefs.ID.IN_TCP_Login] = UserLoginMsg;
            MessageFunctions[PacketDefs.ID.IN_TCP_StartGame] = StartGameMsg;
		}

		public void ParseMessage(string msg)
		{
			// TODO : This needs to be in some kind of co-routine or thread
			Dictionary<string, object> JsonData = null;

			try
			{
				JsonData = (Dictionary<string, object>)fastJSON.JSON.Parse(msg);
			}
			catch (Exception e)
			{
				Logger.Log(string.Format("Exception handled: {0}", e.Message), Logger.LogPrio.Error);
				return;
			}

			// Get the packet ID
			PacketDefs.ID name = (PacketDefs.ID)((long)JsonData["name"]);

			if (MessageFunctions.ContainsKey(name))
			{
				// Use Id to call functor in lookup table
				MessageFunctions[name](JsonData);
			}
			else
			{
				Logger.Log("The packet name does not exist on the server", Logger.LogPrio.Error);
			}
		}

        // ---- Functors ----
        private bool CreateAccountMsg(Dictionary<string, object> json)
        {
            Logger.Log("Got Create account message");

            // Data to extract form this packet
            string name = "";
            string password = "";
            int clientId = -1;

            // Extract the id of the sender
            if (json.ContainsKey("id"))
            {
                clientId = (int)((long)json["id"]);
            }
            else
            {
                Logger.Log("Error Parsing client id in start game packet", Logger.LogPrio.Error);
                return false;
            }

            if (json.ContainsKey("userName"))
            {
                name = (string)json["userName"];
            }

            if (json.ContainsKey("password"))
            {
                password = (string)json["password"];
            }

            // Send Packet with the return value of AddnewUserToDatabase as the argument for success
            PacketDefs.msgPacket returnPack = new PacketDefs.msgPacket();

            if (string.IsNullOrEmpty(name))
            {
                returnPack.msg = "Error: Name is null or empty";
            }
            else if (string.IsNullOrEmpty(password))
            {
                returnPack.msg = "Error: Password is null or empty";
            }
            else
            {
                returnPack.msg = ServerManager.instance.AddNewClientToDatabase(clientId, name, password);
            }

            ServerManager.instance.SendTcp(ServerManager.instance.GetClient(clientId).tcpSocket, fastJSON.JSON.ToJSON(returnPack, PacketDefs.JsonParams()));

            return true;
        }

        private bool UserLoginMsg(Dictionary<string, object> json)
        {
            Logger.Log("Got Login message");

            // Data to extract form this packet
            string name = "";
            string password = "";
            int clientId = -1;

            // Extract the id of the sender
            if (json.ContainsKey("id"))
            {
                clientId = (int)((long)json["id"]);
            }
            else
            {
                Logger.Log("Error Parsing client id in start game packet", Logger.LogPrio.Error);
                return false;
            }

            if (json.ContainsKey("userName"))
            {
                name = (string)json["userName"];
            }

            if (json.ContainsKey("password"))
            {
                password = (string)json["password"];
            }

            // Send Packet with the return value of AddnewUserToDatabase as the argument for success
            PacketDefs.msgPacket returnPack = new PacketDefs.msgPacket();

            // Check this first as it's quicker than hitting database
            if (!ServerManager.instance.ClientExists(clientId))
            {
                string err = string.Format("Tried to create account with unknown client id: {0}", clientId);
                Logger.Log(err, Logger.LogPrio.Error);
                returnPack.success = false;
                returnPack.msg = err;
            }
            else if (ServerManager.instance.GetClient(clientId).loggedIn)
            {
                returnPack.success = false;
                returnPack.msg = "You are already logged in";
            }
            else
            {
                returnPack.msg = ServerManager.instance.Login(name, password, clientId, out returnPack.success);
            }

            ServerManager.instance.SendTcp(ServerManager.instance.GetClient(clientId).tcpSocket, fastJSON.JSON.ToJSON(returnPack, PacketDefs.JsonParams()));

            return true;
        }

        private bool StartGameMsg(Dictionary<string, object> json)
		{
			int clientId = -1;

			// Extract the id of the sender
			if (json.ContainsKey("id"))
			{
				clientId = (int)((long)json["id"]);
			}
			else
			{
				Logger.Log("Error Parsing client id in start game packet", Logger.LogPrio.Error);
			}

			// Check that this id is in our client hash table
			if (!m_ServerManager.ClientExists(clientId))
			{
				Logger.Log(string.Format("Error client id {0} does not exist", clientId), Logger.LogPrio.Error);
				return false;
			}

            // Check that this client is logged in 
            if (Server.ServerManager.instance.GetClient(clientId).loggedIn == false)
            {
                PacketDefs.msgPacket returnPack = new PacketDefs.msgPacket();
                returnPack.msg = "Error: not logged in";
                returnPack.success = false;
                ServerManager.instance.SendTcp(ServerManager.instance.GetClient(clientId).tcpSocket, fastJSON.JSON.ToJSON(returnPack, PacketDefs.JsonParams()));
                return false;
            }
            else
            {
                // Create Level data if no clients (THIS WILL ALL BE MOVED LATER)
                if (!m_GameSimulation.IsGameDataLoaded())
                {
                    m_GameSimulation.LoadLevel(0);
                }

                // We want to add them to the level and send them the level
                m_ServerManager.SetPlayerHandle(clientId, m_GameSimulation.NumObjects());

                // Now add them **** TODO Need position
                GameObject newPlayer = new Player(Vector2.Zero, GameObjectType.Player, m_GameSimulation.NumObjects(), 1, true, clientId);
                m_GameSimulation.AddGameObject(newPlayer);

                // Create Packet to send to other clients already on server with just this player. *note* last param is set to 0 intentionally
                PacketDefs.MultiGameObjectPacket thisClientPacket = new PacketDefs.MultiGameObjectPacket(1);
                thisClientPacket.objects[0] = new PacketDefs.GameObjectPacket(
                    (int)newPlayer.TypeId(), newPlayer.UnqId(), newPlayer.Position.X, newPlayer.Position.Y, 0);
                thisClientPacket.loadLevel = false;

                // Create Packet for list of all clients now to send to new player
                PacketDefs.MultiGameObjectPacket allClientsPacket =
                    new PacketDefs.MultiGameObjectPacket(m_ServerManager.NumClients());
                allClientsPacket.loadLevel = true;

                // Fill it with data
                int i = 0;
                foreach (GameObject p in m_GameSimulation.GetObjects())
                {
                    if (p.TypeId() == GameObjectType.Player)
                    {
                        allClientsPacket.objects[i] = new PacketDefs.GameObjectPacket(
                            (int)p.TypeId(),
                            p.UnqId(),
                            p.Position.X,
                            p.Position.Y,
                            p.IsClient);

                        ++i;
                    }
                }

                GameClient client = m_ServerManager.GetClient(clientId);
                client.inGame = true;   // Prevents duplicates

                // Add to packet the level to load: the client then loads in the geometry data locally
                // Add to packet whether the client should switch to game now

                // 1 - Send any clients on the server the new one that has been added
                string data = fastJSON.JSON.ToJSON(thisClientPacket, PacketDefs.JsonParams());
                foreach (KeyValuePair<int, GameClient> kvp in ServerManager.instance.GetClients())
                {
                    // Make sure it is not this client: He is included in the all clients packet and doesnt need to know
                    // Make sure they are logged : Because they need to be
                    // Make sure they are in game: Because they will get duplicates when they call this function otherwise

                    // This basically allows controls over the different stages, handles whether we are logged in or not and allows 
                    // to drop in the game at any time
                    if (kvp.Key != clientId && kvp.Value.loggedIn && kvp.Value.inGame)
                    {
                        ServerManager.instance.SendTcp(kvp.Value.tcpSocket, data);
                    }
                }

                // 2 - Send the new client his own player details and the other players in the game
                m_ServerManager.SendTcp(client.tcpSocket, fastJSON.JSON.ToJSON(allClientsPacket, PacketDefs.JsonParams()));

                // Set this back for the next new client
                newPlayer.IsClient = 0;
                return true;
            }
		}

		private bool InputMsg(Dictionary<string, object> json)
		{
			long input = -1;
			long clientId = -1;
            long action = -1;

			if (json.ContainsKey("key"))
			{
				input = (long)json["key"];
			}

            if (json.ContainsKey("act"))
            {
                action = (long)json["act"];
            }

            if (json.ContainsKey("id"))
			{
				clientId = (long)json["id"];
			}

			if (clientId == -1 || input == -1)
			{
				Logger.Log("Problem parsing input packet", Logger.LogPrio.Warning);
				return false;
			}

			int playerHandle = m_ServerManager.GetPlayerHandle((int)clientId);
			m_GameSimulation.InputUpdate(playerHandle, (int)input, (int)action);

			return true;
		}
	}
}
