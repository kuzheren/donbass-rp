using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        #region Callbacks
        public void OnServerStarted()
        {
            Utils.Log("Gamemode started!");

            InitRPCCallbacks();

            //new Thread(SpawnBombingPlanes).Start();
        }
        public void OnPlayerDisconnected(Player player, DisconnectInfo disconnectInfo)
        {
            ProcessPlayerDisconnectEvent(player, disconnectInfo);
        }

        public void OnPeerConnected(Player player) { }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        #endregion

        #region Hooked Callbacks
        public bool OnConnectionRequest(ConnectionRequest request, string key)
        {
            if (key != Config.SERVER_KEY)
            {
                // implement outdated version message
            }
            return key == Config.SERVER_KEY;
        }

        public bool OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            return true;
        }

        public bool OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            return true;
        }

        public bool OnInitConnection(Player player, ConnectionRequestInfo connectionRequestInfo, ref Dictionary<string, NetworkValue> customPlayerData)
        {
            ValidateConnectionRequestAndKickIfNot(player, connectionRequestInfo);

            CreateInitGameData(ref customPlayerData);

            return true;
        }

        public bool OnReceivePacket(PacketId packetId, ProtonStream data, Dictionary<string, NetworkValue> customPacketData, DeliveryMethod deliveryMethod)
        {
            return true;
        }

        public bool OnReceiveRPC(int rpcId, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            return true;
        }
        #endregion
    }
}
