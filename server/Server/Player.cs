using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;

namespace ProtonServer
{
    public class Player
    {
        public NetPeer playerPeer;
        public Server server;
        public bool initialized;

        public int Id;
        public string nickname = "";
        public float ping;
        public Dictionary<string, NetworkValue> customPlayerData = new Dictionary<string, NetworkValue>();
        public Dictionary<string, object> properties = new Dictionary<string, object>();

        public Player(NetPeer playerPeer, Server server, int Id)
        {
            this.playerPeer = playerPeer;
            this.server = server;
            this.Id = Id;
        }
        public void ProcessData(ProtonStream data, DeliveryMethod deliveryMethod)
        {
            byte dataId = data.Read<byte>();

            switch (dataId)
            {
                case Identificators.PACKET:
                    Dictionary<string, NetworkValue> customPacketData = data.Read<Dictionary<string, NetworkValue>>();
                    ProcessPacket(data, customPacketData, deliveryMethod);
                    break;

                case Identificators.RPC:
                    ProcessRPC(data, deliveryMethod);
                    break;
            }
        }
        public void SendProtonStream(ProtonStream stream, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            try
            {
                playerPeer.Send(stream.Bytes.ToArray(), deliveryMethod);
            }
            catch (Exception exception)
            {
                Utils.LogError(exception);
            }
        }
        public void SendPacket(ProtonStream stream, Dictionary<string, NetworkValue> customPacketData = null, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            if (customPacketData == null)
            {
                customPacketData = new Dictionary<string, NetworkValue>();
            }

            ProtonStream resultStream = new ProtonStream();
            resultStream.Write(Identificators.PACKET);
            resultStream.Write(customPacketData);
            resultStream.Write(stream);

            SendProtonStream(resultStream, deliveryMethod);
        }
        public void SendRPCPacket(ProtonStream stream, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            ProtonStream resultStream = new ProtonStream();
            resultStream.Write(Identificators.RPC);
            resultStream.Write(stream);

            SendProtonStream(resultStream, deliveryMethod);
        }
        private void ProcessPacket(ProtonStream data, Dictionary<string, NetworkValue> customPacketData, DeliveryMethod deliveryMethod)
        {
            int packetId = data.Read<int>();
            PacketId packet = (PacketId)Enum.ToObject(typeof(PacketId), packetId);

            try
            {
                if (!server.gamemode.OnReceivePacket(packet, data, customPacketData, deliveryMethod))
                {
                    return;
                }
            }
            catch (Exception exception) { Utils.LogError(exception); return; }

            switch (packet)
            {
                case (PacketId.CONNECTION_INFO):
                    if (initialized)
                    {
                        return;
                    }

                    try
                    {
                        ProtonPacketDeserializer connectionInfoDeserializer = new ProtonPacketDeserializer(data, typeof(ConnectionRequestInfo));
                        ConnectionRequestInfo connectionRequestInfo = (ConnectionRequestInfo)connectionInfoDeserializer.structure;

                        this.nickname = connectionRequestInfo.nickname;
                        Dictionary<string, NetworkValue> customPlayerData = new Dictionary<string, NetworkValue>();

                        if (!server.gamemode.OnInitConnection(this, connectionRequestInfo, ref customPlayerData))
                        {
                            server.KickPlayer(this);
                            return;
                        }

                        this.server.serverLogic.AddPlayer(this);

                        SendPlayerClass(this, true);
                        SendPlayersList();
                        SendConnectionAcceptedPacket(customPlayerData);
                        initialized = true;

                        Utils.ServerLog($"Player connected: {connectionRequestInfo.nickname}");
                    }
                    catch (Exception exception) { server.KickPlayer(this); Utils.LogError(exception); }

                    break;

                case (PacketId.CHAT):
                    string message = data.Read<string>();

                    try
                    {
                        if (message[0] == '/')
                        {
                            string fullCommandsText = message.Substring(1);
                            string[] arguments = fullCommandsText.Split();
                            string argumentsString = string.Join(" ", arguments.Skip(1));
                            string[] argumentsList = argumentsString.Split();

                            server.gamemode.OnChatCommand(this, arguments[0], argumentsString, argumentsList);

                            return;
                        }

                        server.gamemode.OnChatMessage(this, message);
                    }
                    catch (Exception exception) { Utils.LogError(exception); }

                    break;
            }
        }
        private void ProcessRPC(ProtonStream data, DeliveryMethod deliveryMethod)
        {
            int rpcId = data.Read<int>();
            int targetId = data.Read<int>();
            int argumentsLength = data.Read<int>();

            List<NetworkValue> arguments = new List<NetworkValue>();
            for (int i = 0; i < argumentsLength; i++)
            {
                NetworkValue rpcValue = data.Read<NetworkValue>();
                arguments.Add(rpcValue);
            }

            try
            {
                if (!server.gamemode.OnReceiveRPC(rpcId, targetId, arguments, deliveryMethod))
                {
                    return;
                }
            }
            catch (Exception exception) { Utils.LogError(exception); return; }

            switch (targetId)
            {
                case ((int)RPCTarget.SERVER):
                    break;

                case ((int)RPCTarget.ALL):
                    server.serverLogic.SendRPC(rpcId, targetId, arguments, deliveryMethod);
                    break;

                case ((int)RPCTarget.OTHERS):
                    server.serverLogic.SendRPCExcept(rpcId, targetId, arguments, this, deliveryMethod);
                    break;

                default:
                    Player targetedPlayer = server.GetPlayerById(targetId);
                    if (targetedPlayer != null)
                    {
                        targetedPlayer.SendRPC(rpcId, deliveryMethod, arguments);
                    }
                    break;
            }

            server.rpcListener.Invoke(rpcId, this, targetId, arguments, deliveryMethod);
        }

        public void SendRPC(int rpcId, DeliveryMethod deliveryMethod, List<NetworkValue> arguments)
        {
            ProtonStream rpcPS = new ProtonStream();
            rpcPS.Write(rpcId);
            rpcPS.Write(arguments.Count);

            foreach (NetworkValue argument in arguments)
            {
                rpcPS.Write(argument);
            }

            SendRPCPacket(rpcPS, deliveryMethod);
        }
        public void SendConnectionAcceptedPacket(Dictionary<string, NetworkValue> customServerData=null)
        {
            if (customServerData == null)
            {
                customServerData = new Dictionary<string, NetworkValue>();
            }

            ConnectionRequestAccepted requestAccepted = new ConnectionRequestAccepted(Config.SERVER_VERSION, Config.GAME_VERSIONS, Config.SERVER_NAME, customServerData);
            ProtonPacketSerializer requestAcceptedSerializer = new ProtonPacketSerializer((int)(PacketId.CONNECTION_ACCEPTED), requestAccepted);

            SendPacket(requestAcceptedSerializer.stream);
        }
        public void SendPlayersList()
        {
            List<Player> players = server.players;
            List<PlayerInfo> playersInfo = new List<PlayerInfo>();

            foreach (Player player in players)
            {
                PlayerInfo playerInfo = new PlayerInfo(player.Id, player.nickname, player.ping, player.customPlayerData);
                playersInfo.Add(playerInfo);
            }

            List<NetworkValue> playersValues = ProtonTypes.ConvertToNetworkValuesList(playersInfo);
            NetworkValue networkPlayersList = new NetworkValue(playersValues);

            ProtonPacketSerializer playersListSerializer = new ProtonPacketSerializer((int)PacketId.PLAYERS_LIST, new object[] { networkPlayersList });
            SendPacket(playersListSerializer.stream);
        }
        public void SendPlayerClass(Player player, bool local)
        {
            Dictionary<string, NetworkValue> customPacketData = new Dictionary<string, NetworkValue>() { { "local", new NetworkValue(local) } };

            PlayerInfo playerInfo = new PlayerInfo(player.Id, player.nickname, player.ping, player.customPlayerData);
            ProtonPacketSerializer playerInfoSerializer = new ProtonPacketSerializer((int)PacketId.PLAYER_CLASS, playerInfo);

            SendPacket(playerInfoSerializer.stream, customPacketData);
        }
        public void SendRemovePlayerClass(Player player)
        {
            ProtonPacketSerializer playerInfoSerializer = new ProtonPacketSerializer((int)PacketId.PLAYER_CLASS_REMOVE, new object[] { player.Id });
            SendPacket(playerInfoSerializer.stream);
        }
        public void SendChatMessage(string message)
        {
            ProtonPacketSerializer chatMessageSerializer = new ProtonPacketSerializer((int)PacketId.CHAT, new object[] { message });
            SendPacket(chatMessageSerializer.stream);
        }

        public void Disconnect()
        {
            playerPeer.Disconnect();
        }

        public static bool operator ==(Player player1, Player player2)
        {
            if (player1 is null || player2 is null)
            {
                return false;
            }

            return player1.Id == player2.Id;
        }
        public static bool operator !=(Player player1, Player player2)
        {
            if (player1 is null || player2 is null)
            {
                return true;
            }

            return player1.Id != player2.Id;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Player))
            {
                return false;
            }

            return this.Id == ((Player)obj).Id;
        }
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
        public override string ToString()
        {
            return $"{this.nickname} ({this.Id})";
        }
    }
}
