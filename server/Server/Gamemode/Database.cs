using LiteNetLib;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private Dictionary<string, string> ExecuteQuery(string query)
        {
            string url;
            if (Config.LOCAL_SERVER)
            {
                url = "http://84.252.75.20/games/donbass-simulator/query.php";
            }
            else
            {
                url = "http://localhost/games/donbass-simulator/query.php";
            }

            NameValueCollection data = new NameValueCollection();
            data.Add("password", Config.QUERY_PASSWORD);
            data.Add("request", query);

            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                byte[] result = client.UploadValues(url, data);
                string resultString = Encoding.UTF8.GetString(result);

                if (resultString.StartsWith("?"))
                {
                    resultString = resultString.Remove(0, 1);
                }

                Dictionary<string, string> resultDict = new Dictionary<string, string>();
                string[] lines = resultString.Split('\n');

                foreach (string line in lines)
                {
                    string[] parts = line.Split(new char[] { ':' }, 2);

                    if (parts.Length == 2)
                    {
                        resultDict[parts[0]] = parts[1];
                    }
                }

                return resultDict;
            }
        }
    }
}
