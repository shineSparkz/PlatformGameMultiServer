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
			PacketDefs.Start();

            // Start broadcasting thread
            Thread bcastThread = new Thread(new ThreadStart(broadCast_t));
			bcastThread.Name = "BroadcastThread";
			bcastThread.Start();

            // Run the game sim loop on another thread
            Thread gameLoopThread = new Thread(new ThreadStart(GameSimulation.instance.Run));
            gameLoopThread.Name = "GameLoopThread";
            gameLoopThread.Start();

            // Listen for new clients and handle incoming packets
            AsynchSocketListener server = new AsynchSocketListener();
			server.StartListeningForNewClients();

            GameSimulation.instance.Shutdown();
            Console.ReadLine();	
		}
	}
}
