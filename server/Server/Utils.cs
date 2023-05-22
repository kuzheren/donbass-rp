using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtonServer
{
    public static class Utils
    {
        public static void Print(object message)
        {
            Console.WriteLine($"{DateTime.Now.ToString("[HH:mm:ss]")} {message}");
        }
        public static void ServerLog(object message)
        {
            Print($"[Server]: {message}");
            WriteLog($"[ERROR]: {message}");
        }
        public static void Log(object message)
        {
            Print($"[Gamemode]: {message}");
            WriteLog($"[Gamemode]: {message}");
        }
        public static void LogError(object message)
        {
            Print($"[ERROR]: {message}");
            WriteLog($"[ERROR]: {message}");
        }
        public static void WriteLog(object message, string logFileName="log.txt")
        {
            return;

            try
            {
                using (StreamWriter writer = File.AppendText($"Logs/{logFileName}"))
                {
                    writer.WriteLine($"{DateTime.Now.ToString("[HH:mm:ss]")} {message}");
                    writer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception exception)
            {
                Print(exception);
            }
        }
        public static int GenerateUniqueId()
        {
            return new Random().Next(int.MinValue, int.MaxValue);
        }
    }
}
