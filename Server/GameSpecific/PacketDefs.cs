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
			CreateObject,
			UpdateObject,
		}

		public class ObjetUpdate
		{

		}
    }
}
