using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Utils;
using Server.GameSpecific;

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
		private bool InputMsg(Dictionary<string, object> json)
		{
			long input = -1;
			long clientId = -1;

			if (json.ContainsKey("input"))
			{
				input = (long)json["input"];
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
			m_GameSimulation.InputUpdate(playerHandle, (int)input);

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

			// Create Level data if no clients (THIS WILL ALL BE MOVED LATER)
			if (!m_GameSimulation.IsGameDataLoaded())
			{
				m_GameSimulation.LoadLevel(0);
			}

			// We want to add them to the level and send them the level
			m_ServerManager.SetPlayerHandle(clientId, m_GameSimulation.NumObjects());

			// Now add them
			GameObject newPlayer = new GameObject(GameObjectType.Player, m_GameSimulation.NumObjects());
			newPlayer.isClientPlayer = 1;
			m_GameSimulation.AddGameObject(newPlayer);

			// Create Packet to send to other clients already on server with just this player. *note* last param is set to 0 intentionally
			PacketDefs.MultiGameObjectPacket thisClientPacket = new PacketDefs.MultiGameObjectPacket(1);
			thisClientPacket.objects[0] = new PacketDefs.GameObjectPacket(
				(int)newPlayer.object_id, newPlayer.unique_id, newPlayer.Position.X, newPlayer.Position.Y, 0);

			// Create Packet for list of all clients now to send to new player
			PacketDefs.MultiGameObjectPacket allClientsPacket =
				new PacketDefs.MultiGameObjectPacket(m_ServerManager.NumClients());

			// Fill it with data
			int i = 0;
			foreach (GameObject p in m_GameSimulation.GetObjects())
			{
				if (p.object_id == GameObjectType.Player)
				{
					allClientsPacket.objects[i] = new PacketDefs.GameObjectPacket(
						(int)p.object_id,
						p.unique_id,
						p.Position.X,
						p.Position.Y,
						p.isClientPlayer);

					++i;
				}
			}

			GameClient client = m_ServerManager.GetClient(clientId);

			// 1 - Send any clients on the server the new one that has been added
			m_ServerManager.SendAllTcpExcept(fastJSON.JSON.ToJSON(thisClientPacket, PacketDefs.JsonParams()), clientId);

			// 2 - Send the new client his own player details and the other players in the game
			m_ServerManager.SendTcp(client.tcpSocket, fastJSON.JSON.ToJSON(allClientsPacket, PacketDefs.JsonParams()));

			// Set this back for the next new client
			newPlayer.isClientPlayer = 0;
			return true;
		}
	}
}
