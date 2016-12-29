using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.GameSpecific
{
    public class PacketDefs
    {
		public enum PacketID
		{
			Register_Tcp,
			GamePlayerObjects_Tcp,

            UpdateInput_Udp,
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
        [Serializable()]
		public class basepacket
		{
			public int name { get; set; }
		}

		[Serializable()]
		public class regPacket : basepacket
		{
			public int clientId { get; set; }
			public int udpPort { get; set; }

			public regPacket()
			{
			}

			public regPacket(int clientId, int udpPort)
			{
				this.name = (int)PacketDefs.PacketID.Register_Tcp;
				this.clientId = clientId;
				this.udpPort = udpPort;
			}
		}

        [Serializable()]
        public class MultiGameObjectPacket : basepacket
        {
            public GameObjectPacket[] objects = null;

            public MultiGameObjectPacket(int numClients)
            {
                this.name = (int)PacketDefs.PacketID.GamePlayerObjects_Tcp;
                this.objects = new GameObjectPacket[numClients];
            }
        }

        [Serializable()]
        public class PlayerInputUpdatePacket : basepacket
        {
            public int handle;
            public float px;
            public float py;

            public PlayerInputUpdatePacket(int handle, float x, float y)
            {
                this.name = (int)PacketDefs.PacketID.UpdateInput_Udp;
                this.handle = handle;
                this.px = x;
                this.py = y;
            }
        }

        #endregion
    }
}
