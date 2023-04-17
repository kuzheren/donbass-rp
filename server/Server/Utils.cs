using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtonServer
{
    public static class Utils
    {
        public static void ServerLog(object message)
        {
            Console.WriteLine($"[Server]: {message}");
        }
        public static void Log(object message)
        {
            Console.WriteLine($"[Gamemode]: {message}");
        }
        public static void LogError(object message)
        {
            Console.WriteLine($"[ERROR]: {message}");
        }
        public static int GenerateUniqueId()
        {
            return new Random().Next(int.MinValue, int.MaxValue);
        }
    }
}
