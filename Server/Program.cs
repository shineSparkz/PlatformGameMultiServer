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
	/*
        TODO
        - The idea of server knowing which level to load
        - SQL data
        - Store clients in database
        - Security
        
        - Persisitance, say for exp or something
        - Update game state on server (like physics)

        DONE
        - JSON proto
		- Sort all static stuff
        - Separate starting a game and connecting

        
    */

	class Program
	{
		public static void broadCast_t()
		{
			bool done = false;

			UdpClient listener = new UdpClient(ServerDefs.BROADCAST_PORT);
			IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, ServerDefs.BROADCAST_PORT);

			try
			{
				while (!done)
				{
					Console.WriteLine("Waiting for broadcast");

					// Block and wait for broadcast queery
					byte[] buff = listener.Receive(ref groupEP);

					Console.WriteLine("Received broadcast from {0}", groupEP.ToString());

					Byte[] msg = Encoding.ASCII.GetBytes("Broadcast");

					// The client will get our IP Address here
					listener.Send(msg, msg.Length, groupEP);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception {0}", e.Message);
			}
			finally
			{
				listener.Close();
			}
		}

        static void Main(string[] args)
		{
			/*
			//string jrp = "{ \"name\" : 0, \"age\" : 29 } ";

			// Pack into JSON like this to send out

			PacketDefs.regPacket rp = new PacketDefs.regPacket(PacketDefs.PacketID.Register, 0, 2456);
			string jrp = fastJSON.JSON.ToJSON(rp);

			Dictionary<string, object> JDATA = (Dictionary<string, object>)fastJSON.JSON.Parse(jrp);

			if (JDATA.ContainsKey("name"))
			{
				try
				{
					//JDATA["name"]
					long name = (long)JDATA["name"];

					if (name == (int)PacketDefs.PacketID.Register)
					{
						
					}
				}
				catch (Exception e)
				{
					string err = e.Message;
					Console.WriteLine(err);
				}
			}
            */

			PacketDefs.Start();

            /*
			PacketDefs.mapPacket map = new PacketDefs.mapPacket();
			map.Objects.Add(new PacketDefs.GameObjectPacket(0, 1, 2.0f, 4.0f, false));
			map.Objects.Add(new PacketDefs.GameObjectPacket(0, 2, 4.0f, 7.0f, true));

			string jd = fastJSON.JSON.ToJSON(map, PacketDefs.JsonParams());

			Dictionary<string, object> JDATA = (Dictionary<string, object>)fastJSON.JSON.Parse(jd);
			*/

            /*
            // Create Packet for list of all clients
            PacketDefs.MultiGameObjectPacket allClientsPacket =
                new PacketDefs.MultiGameObjectPacket(2);

            allClientsPacket.objects[0] = new PacketDefs.GameObjectPacket(
                0,1,0,0,1);
            allClientsPacket.objects[1] = new PacketDefs.GameObjectPacket(
            0, 1, 0,30, 2);

            string jd = fastJSON.JSON.ToJSON(allClientsPacket, PacketDefs.JsonParams());
            Dictionary<string, object> JDATA = (Dictionary<string, object>)fastJSON.JSON.Parse(jd);
            */

            // Start broadcasting thread
            Thread bcastThread = new Thread(new ThreadStart(broadCast_t));
			bcastThread.Name = "BroadcastThread";
			bcastThread.Start();

            // Server manager, looks after clients and sending out data
            ServerManager serverManager = new ServerManager();

            // Game sim is for rhe updating of the game and collisions etc
            GameSimulation gameSim = new GameSimulation(serverManager);
            serverManager.SetGameSim(gameSim);

            // Run the game sim loop on another thread
            Thread gameLoopThread = new Thread(new ThreadStart(gameSim.Run));
            gameLoopThread.Name = "GameLoopThread";
            gameLoopThread.Start();

            // Listen for new clients and handle incoming packets
            AsynchSocketListener server = new AsynchSocketListener(serverManager, gameSim);
			server.StartListeningForNewClients();

            gameSim.Shutdown();
            Console.ReadLine();	
		}
	}
}
