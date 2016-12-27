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

    /*
        TODO
        - JSON proto
        - SQL data
        - Store clients in database
        - Security
        - Persisitance, say for exp or something
        - Separate starting a game and connecting
        - Hashing messages id's rather than strings
        - The idea of server knowing which level to load
        - Update game state on server (like physics)
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
			//string json = "{ \"name\" : \"alex\", \"age\" : 29 } ";

			// Pack into JSON like this to send out

			/*
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
            */

			

			// Start broadcasting thread
			Thread bcastThread = new Thread(new ThreadStart(broadCast_t));
			bcastThread.Name = "BroadcastThread";
			bcastThread.Start();
			
			AsynchSocketListener.StartListeningForNewClients();
			Console.ReadLine();	
		}
	}
}
