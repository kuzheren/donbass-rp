using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtonServer
{
    public class ConnectionRequestInfo : IProtonSerializable
    {
        public string nickname = "";
        public string version = "";
        public string deviceId = "";
        public Dictionary<string, NetworkValue> customData = new Dictionary<string, NetworkValue>();

        public ConnectionRequestInfo() { }
        public ConnectionRequestInfo(string nickname, string version, string deviceId, Dictionary<string, NetworkValue> customData = null)
        {
            if (customData == null)
            {
                customData = new Dictionary<string, NetworkValue>();
            }
            if (nickname == null)
            {
                nickname = "";
            }

            this.nickname = nickname;
            this.version = version;
            this.deviceId = deviceId;
            this.customData = customData;
        }

        public void DeserializeFromStream(ProtonStream stream)
        {
            this.nickname = stream.Read<string>();
            this.version = stream.Read<string>();
            this.deviceId = stream.Read<string>();
            this.customData = stream.Read<Dictionary<string, NetworkValue>>();
        }
        public void SerializeToStream(ProtonStream stream)
        {
            stream.Write(this.nickname);
            stream.Write(this.version);
            stream.Write(this.deviceId);
            stream.Write(this.customData);
        }
    }
    public class ConnectionRequestAccepted : IProtonSerializable
    {
        public string serverVersion = "";
        public string gameVersion = "";
        public string serverName = "";
        public Dictionary<string, NetworkValue> customData = new Dictionary<string, NetworkValue>();

        public ConnectionRequestAccepted() { }
        public ConnectionRequestAccepted(string serverVersion, string gameVersion, string serverName, Dictionary<string, NetworkValue> customData = null)
        {
            if (customData == null)
            {
                customData = new Dictionary<string, NetworkValue>();
            }

            this.serverVersion = serverVersion;
            this.gameVersion = gameVersion;
            this.serverName = serverName;
            this.customData = customData;
        }

        public void DeserializeFromStream(ProtonStream stream)
        {
            this.serverVersion = stream.Read<string>();
            this.gameVersion = stream.Read<string>();
            this.serverName = stream.Read<string>();
            this.customData = stream.Read<Dictionary<string, NetworkValue>>();
        }
        public void SerializeToStream(ProtonStream stream)
        {
            stream.Write(this.serverVersion);
            stream.Write(this.gameVersion);
            stream.Write(this.serverName);
            stream.Write(this.customData);
        }
    }
    public class PlayerInfo : IProtonSerializable
    {
        public int Id = -1;
        public string nickname = "";
        public float ping = 0.0f;
        public Dictionary<string, NetworkValue> customData = new Dictionary<string, NetworkValue>();

        public PlayerInfo() { }
        public PlayerInfo(int Id, string nickname, float ping, Dictionary<string, NetworkValue> customData = null)
        {
            if (customData == null)
            {
                customData = new Dictionary<string, NetworkValue>();
            }

            this.Id = Id;
            this.nickname = nickname;
            this.ping = ping;
            this.customData = customData;
        }

        public void DeserializeFromStream(ProtonStream stream)
        {
            this.Id = stream.Read<int>();
            this.nickname = stream.Read<string>();
            this.ping = stream.Read<float>();
            this.customData = stream.Read<Dictionary<string, NetworkValue>>();
        }
        public void SerializeToStream(ProtonStream stream)
        {
            stream.Write(this.Id);
            stream.Write(this.nickname);
            stream.Write(this.ping);
            stream.Write(this.customData);
        }
    }
}
