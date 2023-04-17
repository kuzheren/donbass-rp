using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtonServer
{
    public static class ProtonServer
    {
        static void Main()
        {
            Server server = new Server(Config.PORT);
            Utils.ServerLog($"Server started at {Config.IP}:{Config.PORT}");
        }
    }
}