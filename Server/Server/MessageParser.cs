using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Server.Utils;
using Server.GameSpecific;
using Server.GameSpecific.GameObjects;

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
			MessageFunctions[PacketDefs.ID.IN_TCP_ExpQueery] = ExpQueeryMsg;
            MessageFunctions[PacketDefs.ID.IN_TCP_LeaderboardRequest] = LeaderboardRequestMsg;
        }

        public void ParseMessage(string msg)
		{
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

        // ---- Function Pointers ----
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

			// Send the info as udp
			ServerManager.instance.SendUdp(clientId, fastJSON.JSON.ToJSON(returnPack, PacketDefs.JsonParams()));

            // Send them an exp update
            GameClient thisClient = ServerManager.instance.GetClient(clientId);

            if (thisClient != null)
            {

                int expFromDB = ServerManager.instance.GetClientExp(name);
                thisClient.localExpCache = expFromDB;

                PacketDefs.UpdateExpPacket flp = new PacketDefs.UpdateExpPacket(expFromDB, PacketDefs.ID.OUT_TCP_ExpQueery);
                ServerManager.instance.SendTcp(ServerManager.instance.GetClient(clientId).tcpSocket, fastJSON.JSON.ToJSON(flp, PacketDefs.JsonParams()));
                return true;
            }
            else
            {
                return false;
            }
        }

		private bool StartGameMsg(Dictionary<string, object> json)
		{
			// This is complicated so will do my best to comment, even for myself in the future
			// There are 4 players allowed to drop in/out per game, this tries to cover all possible scenarios that I could think of

			/*
			 * There are a number of different conditionns that need to be considered
			 * - The player needs to have an account and be logged in to start a game
			 * - If the above conditions are not met, he will simply get an error message explainig why
			 * - This may be the first play that starts the game, if so the level will be constructed, he will be sent a packet for info on his playe
			 * - If there are already clients online, then they need to know about this new player who has joined, and he needs to know about himself and them
			 * - Next... need to consider the fact that the other clients on the server are in game or not, if they are not in a game, and in the lobby then they
			 *   do NOT need to know about this new player until they have started their own game
			 * - If all players reach the exit, then the game simulation is cleared from memory
			 * - Up to 4 players can join the same game at any time, and the game session is considered over when all players have either reached the goal or quit out
			 * - In an edge case situation.... two players start a game >> player 1 finished level and goes back to lobby >> player 2 is still playing >> player 1 can re-enter 
			 *   using the same internal game object, the player count will still  be incremented as this could go on forever otherwise
			*/

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

			// Client must be logged in!!
			if (Server.ServerManager.instance.GetClient(clientId).loggedIn == false)
			{
				PacketDefs.msgPacket returnPack = new PacketDefs.msgPacket();
				returnPack.msg = "Error: not logged in";
				returnPack.success = false;
				ServerManager.instance.SendTcp(ServerManager.instance.GetClient(clientId).tcpSocket, fastJSON.JSON.ToJSON(returnPack, PacketDefs.JsonParams()));
				return false;
			}
			// Game must not be full !!
			else if (m_GameSimulation.GetPlayersInGame() >= 4)
			{
				PacketDefs.msgPacket returnPack = new PacketDefs.msgPacket();
				returnPack.msg = "Game is full please wait";
				returnPack.success = false;
				ServerManager.instance.SendTcp(ServerManager.instance.GetClient(clientId).tcpSocket, fastJSON.JSON.ToJSON(returnPack, PacketDefs.JsonParams()));
				return false;
			}
			else
			{
				// This is first client so load level data
				if (!m_GameSimulation.IsGameDataLoaded())
				{
					m_GameSimulation.LoadLevel(0);
				}

				GameClient thisClient = ServerManager.instance.GetClient(clientId);

				// Check if he is re-entering the same game
				if (thisClient.playerObjectHandle > -1)
				{
					// Do we have a match
					foreach (GameObject go in GameSimulation.instance.GetPlayers())
					{
						if (go.UnqId() == thisClient.playerObjectHandle && go.Active == false)
						{
							// The players already on will get an update to activate this in next tick
							go.Active = true;

							// Increment counter
							GameSimulation.instance.AddNewPlayerToSession();
						}
					}
				}
				// Weird edge case: Previous client quit, but we still have a re-useable game object in memory so give him that
				else if (GameSimulation.instance.GetPlayers().Count == ServerManager.instance.NumClients())
				{
					foreach (GameObject go in GameSimulation.instance.GetPlayers())
					{
						if (go.Active == false)
						{
							// The players already on will get an update to activate this in next tick
							go.Active = true;

							// Increment counter
							GameSimulation.instance.AddNewPlayerToSession();

							// Set the internal memoory handle store in client data
							m_ServerManager.SetPlayerHandle(clientId, go.UnqId());

							go.IsClient = 1;
						}
					}
				}
				else
				{
					// This client is new to this game

					// Set the internal memoory handle store in client data
					m_ServerManager.SetPlayerHandle(clientId, m_GameSimulation.NumObjects());

                    // Add new client to game sim
                        
                    GameObject newPlayer = new Player(Player.SpawnPosition(), GameObjectType.Player, m_GameSimulation.NumObjects(), 1, true, clientId,
                        new Vector2(96, 96), new ColliderOffset(26, 26, 30, 0));  
                        //new Vector2(128, 128), new ColliderOffset(46,46,50,0));
                    m_GameSimulation.AddGameObject(newPlayer);

					// Create Packet of one game object (this player) to send to other clients already on server with just this player. *note* last param (isClient) is set to 0 intentionally
					PacketDefs.MultiGameObjectPacket thisClientPacket = new PacketDefs.MultiGameObjectPacket(1);
					thisClientPacket.objects[0] = new PacketDefs.GameObjectPacket(
						(int)newPlayer.TypeId(), newPlayer.UnqId(), newPlayer.Position.X, newPlayer.Position.Y, 0);

					// This flag signifies if we actually need to  load the level,m because the other client is either in game or lobby then we don't want this
					thisClientPacket.loadLevel = false;

					// 1 - Send any clients on the server the new one that has been added
					string data = fastJSON.JSON.ToJSON(thisClientPacket, PacketDefs.JsonParams());
					foreach (KeyValuePair<int, GameClient> kvp in ServerManager.instance.GetClients())
					{
						// - Make sure it is not this client: He is included in the all clients packet and doesnt need to know
						// - Make sure they are logged : Because they need to be
						// - Make sure they are in game: Because they will get duplicates when they call this function otherwise

						// This basically allows controls over the different stages, handles whether we are logged in or not and allows 
						// to drop in the game at any time
						if (kvp.Key != clientId && kvp.Value.loggedIn && kvp.Value.inGame)
						{
							ServerManager.instance.SendTcp(kvp.Value.tcpSocket, data);
						}
					}
				}

				// Create Packet for list of all clients now to send to new player including himself
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
							p.IsClient
						);

						++i;
					}
				}

				// Prevents duplicates
				thisClient.inGame = true;   

				// 2 - Send the new client his own player details and the other players in the game
				m_ServerManager.SendTcp(thisClient.tcpSocket, fastJSON.JSON.ToJSON(allClientsPacket, PacketDefs.JsonParams()));

				// Set all players 'isClient' flag to null now that packets are sent
				foreach (GameObject player in GameSimulation.instance.GetPlayers())
				{
					player.IsClient = 0;
				}
                
				return true;
			}
		}

		private bool ExpQueeryMsg(Dictionary<string, object> json)
		{
			// Just send them back the request
			int clientId = -1;
			if (json.ContainsKey("id"))
			{
				clientId = (int)((long)json["id"]);
			}

			GameClient client = ServerManager.instance.GetClient(clientId);

			if (client != null)
			{
				PacketDefs.UpdateExpPacket flp = new PacketDefs.UpdateExpPacket(ServerManager.instance.GetClientExp(client.userName), PacketDefs.ID.OUT_TCP_ExpQueery);
				ServerManager.instance.SendTcp(ServerManager.instance.GetClient(clientId).tcpSocket, fastJSON.JSON.ToJSON(flp, PacketDefs.JsonParams()));
				return true;
			}

			return false;
		}

        private bool LeaderboardRequestMsg(Dictionary<string, object> json)
        {
            // Just send them back the request
            int clientId = -1;
            if (json.ContainsKey("id"))
            {
                clientId = (int)((long)json["id"]);
            }

            GameClient client = ServerManager.instance.GetClient(clientId);

            if (client != null)
            {
                PacketDefs.LeaderboardPacket lbp = new PacketDefs.LeaderboardPacket(ServerManager.instance.GetLeaderBoard());
                ServerManager.instance.SendTcp(ServerManager.instance.GetClient(clientId).tcpSocket, fastJSON.JSON.ToJSON(lbp, PacketDefs.JsonParams()));
                return true;
            }

            return false;
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
			m_GameSimulation.InputUpdate(playerHandle, (int)input, (int)action, (int)clientId);

			return true;
		}
	}
}
