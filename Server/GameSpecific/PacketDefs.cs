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
			Register,
			SendMap,
			UpdateObject,
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

			public regPacket(PacketID name, int clientId, int udpPort)
			{
				this.name = (int)name;
				this.clientId = clientId;
				this.udpPort = udpPort;
			}
		}

		[Serializable()]
		public class GameObjectPacket : basepacket
		{
			public int oid;			// Obj Id
			public int uid;			// Unique Id
			public float px;		// Position x	
			public float py;		// Position y
			public int clt;			// Is Client

			public GameObjectPacket(int objId, int uniqId, float px, float py, int isClient)
			{
				this.name = (int)PacketDefs.PacketID.SendMap;
				this.oid = objId;
				this.uid = uniqId;
				this.px = px;
				this.py = py;
				this.clt = isClient;
			}
		}
    }
}
