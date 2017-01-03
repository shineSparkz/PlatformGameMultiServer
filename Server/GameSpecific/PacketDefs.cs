﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameSpecific
{
    public class PacketDefs
    {
		public enum ID
		{
			// TCP out
			OUT_TCP_Connect,
            OUT_TCP_ServerMsg,
			OUT_TCP_StartGame,

			// UDP out
			OUT_UDP_UpdatedObject,

			// TCP in
            IN_TCP_CreateAccount,
            IN_TCP_Login,
			IN_TCP_StartGame,

			// UDP oin
			IN_UDP_Input,
		}	

		private static fastJSON.JSONParameters m_JsonParams = new fastJSON.JSONParameters();
		
		public static void Start()
		{
		    m_JsonParams.SerializeNullValues = false;
			m_JsonParams.UseExtensions = false;
			m_JsonParams.UseUTCDateTime = false;
			m_JsonParams.UsingGlobalTypes = false;
			m_JsonParams.UseFastGuid = false;
		}

		public static fastJSON.JSONParameters JsonParams()
		{
			return m_JsonParams;
		}

        // Note this is in a list, it shouldnt be used as as packet
        [Serializable()]
        public class GameObjectPacket
        {
            public int oid;         // Obj Id
            public int uid;         // Unique Id
            public float px;        // Position x	
            public float py;        // Position y
            public int clt;         // Is Client

            public GameObjectPacket(int objId, int uniqId, float px, float py, int isClient)
            {
                this.oid = objId;
                this.uid = uniqId;
                this.px = px;
                this.py = py;
                this.clt = isClient;
            }
        }

        #region Packets
		// ---- These are only relating to OUT packets ----
        [Serializable()]
		public class basepacket
		{
			public int name { get; set; }
		}

		[Serializable()]
		public class connectPacket : basepacket
		{
			public int clientId { get; set; }
			public int udpPort { get; set; }

			public connectPacket(int clientId, int udpPort)
			{
				this.name = (int)ID.OUT_TCP_Connect;
				this.clientId = clientId;
				this.udpPort = udpPort;
			}
		}

        // --- This can be used for alerts and messages----
        [Serializable()]
        public class msgPacket : basepacket
        {
            public bool success = true;
            public string msg = "";

            public msgPacket()
            {
                this.name = (int)ID.OUT_TCP_ServerMsg; ;
            }
        }


        [Serializable()]
        public class MultiGameObjectPacket : basepacket
        {
            public GameObjectPacket[] objects = null;
            public bool loadLevel = false;

            public MultiGameObjectPacket(int numClients)
            {
				this.name = (int)ID.OUT_TCP_StartGame;
                this.objects = new GameObjectPacket[numClients];
            }
        }

        [Serializable()]
        public class PlayerInputUpdatePacket : basepacket
        {
            public int handle;
            public float px;    // Position X
            public float py;    // Position Y
            public float fx;    // Frame X
            public float fy;    // Frame Y

            public PlayerInputUpdatePacket(int handle, float px, float py, float fx, float fy)
            {
				this.name = (int)ID.OUT_UDP_UpdatedObject;
                this.handle = handle;
                this.px = px;
                this.py = py;
                this.fx = fx;
                this.fy = fy;
            }
        }

        #endregion
    }
}
