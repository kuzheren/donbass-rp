using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private void SaveDBVar(Player player, DBVar key, string info)
        {
            SaveDBVar(player, key.ToString(), info);
        }
        private string GetDBVar(Player player, DBVar key)
        {
            return GetDBVar(player, key.ToString());
        }
        private void SaveDBVar(Player player, string key, string info)
        {
            string token = GetPlayerToken(player);
            string url;

            if (Config.LOCAL_SERVER)
            {
                url = "http://84.252.75.20/games/donbass-simulator/prefs.php";
            }
            else
            {
                url = "http://localhost/games/donbass-simulator/prefs.php";
            }

            string data = $"token={token}&key={key}&value={info}";

            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                client.UploadString(url, "POST", data);
            }
        }
        private string GetDBVar(Player player, string key)
        {
            string token = GetPlayerToken(player);
            string url;

            if (Config.LOCAL_SERVER)
            {
                url = "http://84.252.75.20/games/donbass-simulator/prefs.php";
            }
            else
            {
                url = "http://localhost/games/donbass-simulator/prefs.php";
            }

            using (WebClient client = new WebClient())
            {
                string result = client.DownloadString($"{url}?token={token}&key={key}");
                if (result != "NULL")
                {
                    return result;
                }
            }
            return null;
        }
    }
}
