using IniParser;
using IniParser.Model;
using System.Linq;

namespace ProtonServer
{
    public static class Config
    {
        public static string IP = "0.0.0.0";
        public static int PORT = 50000;
        public static int MAX_PLAYERS = 20;
        public static string SERVER_VERSION = "open beta 1.4";
        public static string GAME_VERSIONS = "27.04.2023.b;26.04.2023.b";
        public static char CMD_PREFIX = '/';
        public static string SERVER_NAME = "Donbass RP";
        public static string SERVER_KEY = "donbass-rp";
        public static int MAX_BULLET_HOLES = 1000;
        public static bool LOCAL_SERVER = false;
        public static string ADMIN_PASSWORD = ")245k34oKIWTJKwoijw58$(@(#$#)53kisdfgsdoP";
        public static string QUERY_PASSWORD = ")245k34oKIWTJKwoijw58$(@(#$#)53kisdfgsdoP";

        private static readonly string configFilePath = "config.ini";
        private static readonly FileIniDataParser parser = new FileIniDataParser();

        public static void Reload()
        {
            IniData data = parser.ReadFile(configFilePath);
            Dictionary<string, string> sectionData = data.Sections["Server"].ToDictionary(x => x.KeyName, x => x.Value);
            IP = GetValueOrDefault(sectionData, "ip", IP);
            PORT = GetValueOrDefault(sectionData, "port", PORT);
            MAX_PLAYERS = GetValueOrDefault(sectionData, "max_players", MAX_PLAYERS);
            SERVER_VERSION = GetValueOrDefault(sectionData, "server_version", SERVER_VERSION);
            GAME_VERSIONS = GetValueOrDefault(sectionData, "game_versions", GAME_VERSIONS);
            CMD_PREFIX = GetValueOrDefault(sectionData, "cmd_prefix", CMD_PREFIX);
            SERVER_NAME = GetValueOrDefault(sectionData, "server_name", SERVER_NAME);
            SERVER_KEY = GetValueOrDefault(sectionData, "server_key", SERVER_KEY);
            MAX_BULLET_HOLES = GetValueOrDefault(sectionData, "max_bullet_holes", MAX_BULLET_HOLES);
            LOCAL_SERVER = GetValueOrDefault(sectionData, "local_server", LOCAL_SERVER);
            ADMIN_PASSWORD = GetValueOrDefault(sectionData, "admin_password", ADMIN_PASSWORD);
            QUERY_PASSWORD = GetValueOrDefault(sectionData, "query_password", QUERY_PASSWORD);
        }
        private static T GetValueOrDefault<T>(Dictionary<string, string> dict, string key, T defaultValue)
        {
            if (dict.TryGetValue(key, out string value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch { }
            }
            return defaultValue;
        }
    }
}
