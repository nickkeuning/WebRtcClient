using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRtcClient.Utilities
{
    class Logger
    {
        public static event Action<string> WriteMessage;

        public static void Log(string message)
        {
            WriteMessage?.Invoke(message);
        }
    }
}
