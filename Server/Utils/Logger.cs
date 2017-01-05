using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Utils
{
	class Logger
	{
		public enum LogPrio
		{
			Info,
			Warning,
			Error
		};

		public static void Log(string msg, LogPrio prio = LogPrio.Info)
		{
			Console.WriteLine(prio.ToString() + " : " + msg);

			// TODO : Write a log file , have settings for this
		}
	}
}
