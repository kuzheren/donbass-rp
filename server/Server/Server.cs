using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;

namespace ProtonServer
{
    /// <summary>
    /// Главный класс сервера.
    /// Можно создавать несколько классов в одном проекте для мультисерверности.
    /// </summary>
    public class Server : INetEventListener
    {
        public ServerLogic serverLogic;
        public Gamemode gamemode;
        public RpcListener rpcListener;
        public NetManager netManager;
        public List<Player> players = new List<Player>();

        /// <summary>
        /// Главный и единственный конструктор сервера.
        /// </summary>
        /// <param name="port"></param>
        public Server(int port)
        {
            netManager = new NetManager(this);
            netManager.DisconnectTimeout = 15000;
            netManager.Start(port);
            new Thread(UpdateEventsThread).Start();

            serverLogic = new ServerLogic(this);

            rpcListener = new RpcListener();

            gamemode = new Gamemode(this, serverLogic);
            gamemode.OnServerStarted();
        }

        /// <summary>
        /// Поток для обновления событий сервера. Обновляет состояния 1000 раз в секунду.
        /// </summary>
        private void UpdateEventsThread()
        {
            while (true)
            {
                netManager.PollEvents();
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Получает класс игрока по его пиру.
        /// </summary>
        /// <returns>Экземпляр игрока</returns>
        public Player GetPlayerByPeer(NetPeer peer)
        {
            foreach (Player player in players)
            {
                if (player.playerPeer == peer)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Получает класс игрока по его Id.
        /// </summary>
        /// <returns>Экземпляр игрока</returns>
        public Player GetPlayerById(int Id)
        {
            foreach (Player player in players)
            {
                if (player.Id == Id)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Получает класс игрока по его IP.
        /// </summary>
        /// <returns>Экземпляр игрока</returns>
        public Player GetPlayerByEndPoint(IPEndPoint endPoint)
        {
            foreach (Player player in players)
            {
                if (player.playerPeer.EndPoint == endPoint)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Отключает игрока от сервера с указанной причиной.
        /// </summary>
        /// <param name="player"></param>
        public void DisconectPlayer(Player player, DisconnectInfo disconnectInfo)
        {
            if (!players.Contains(player))
            {
                return;
            }

            serverLogic.RemovePlayer(player);
            players.Remove(player);
            player.Disconnect();

            Utils.ServerLog($"Player disconnected: {player.nickname}. Reason: {disconnectInfo.Reason}");

            try
            {
                gamemode.OnPlayerDisconnected(player, disconnectInfo);
            }
            catch (Exception exception) { Utils.LogError(exception); }
        }

        /// <summary>
        /// Отключает игрока от сервера, но в причине указывается Reason.ConnectionRejected
        /// </summary>
        public void KickPlayer(Player player)
        {
            DisconectPlayer(player, new DisconnectInfo() { Reason = DisconnectReason.ConnectionRejected});
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            Utils.ServerLog($"Connection request from: {request.RemoteEndPoint}");
            string key = null;

            try
            {
                try
                {
                    key = request.Data.GetString();
                }
                catch
                {
                    request.Reject();
                }

                new Thread(() =>
                {
                    if (gamemode.OnConnectionRequest(request, key))
                    {
                        request.Accept();
                    }
                    else
                    {
                        request.Reject();
                    }
                }
                ).Start();
            }
            catch (Exception exception) { Utils.LogError(exception); request.Reject(); }
        }
        public void OnPeerConnected(NetPeer peer)
        {
            int nextId = 1;
            foreach (Player p in players.OrderBy(p => p.Id))
            {
                if (p.Id == nextId)
                {
                    nextId++;
                }
                else
                {
                    break;
                }
            }

            Player newPlayer = new Player(peer, this, nextId);
            players.Add(newPlayer);

            try
            {
                gamemode.OnPlayerConnected(newPlayer);
            }
            catch (Exception exception) { Utils.LogError(exception); }
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Player quitedPlayer = GetPlayerByPeer(peer);
            if (quitedPlayer != null)
            {
                DisconectPlayer(quitedPlayer, disconnectInfo);
            }
        }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            if (peer == null)
            {
                return;
            }

            try
            {
                if (!gamemode.OnNetworkReceive(peer, reader, channelNumber, deliveryMethod))
                {
                    return;
                }
            }
            catch (Exception exception) { Utils.LogError(exception); return; }

            Player player = GetPlayerByPeer(peer);
            if (player != null && reader != null)
            {
                try
                {
                    player.ProcessData(new ProtonStream(reader.GetRemainingBytes()), deliveryMethod);
                }
                catch (Exception exception)
                {
                    gamemode.OnClientProcessingException(peer, reader, exception);
                    Utils.LogError(exception);
                }
            }
        }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            try
            {
                if (!gamemode.OnNetworkLatencyUpdate(peer, latency))
                {
                    return;
                }

                Player player = GetPlayerByPeer(peer);
                if (player != null)
                {
                    player.ping = latency;
                }
            }
            catch (Exception exception) { Utils.LogError(exception); return; }
        }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            try
            {
                gamemode.OnNetworkError(endPoint, socketError);
            }
            catch (Exception exception) { Utils.LogError(exception); }
        }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            try
            {
                gamemode.OnNetworkReceiveUnconnected(remoteEndPoint, reader, messageType);
            }
            catch (Exception exception) { Utils.LogError(exception); }
        }
    }
    public class RpcListener
    {
        private Dictionary<int, List<RpcCallback>> rpcListeners = new Dictionary<int, List<RpcCallback>>();
        public delegate void RpcCallback(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod);

        /// <summary>
        /// Добавляет функцию в список слушателей некоторого RPC по его ID.
        /// </summary>
        public void AddCallback(int rpcId, RpcCallback callback)
        {
            if (!rpcListeners.ContainsKey(rpcId))
            {
                rpcListeners[rpcId] = new List<RpcCallback>();
            }

            if (rpcListeners[rpcId].Contains(callback))
            {
                return;
            }

            rpcListeners[rpcId].Add(callback);
        }

        /// <summary>
        /// Добавляет функцию в список слушателей некоторого RPC по его строковому ID.
        /// </summary>
        public void AddCallback(string rpcId, RpcCallback callback)
        {
            AddCallback(GetStringHash(rpcId), callback);
        }

        /// <summary>
        /// Удаляет функцию из списка слушателей некоторого RPC по его ID.
        /// </summary>
        public void RemoveListener(int rpcId, RpcCallback callback)
        {
            if (rpcListeners.ContainsKey(rpcId))
            {
                if (rpcListeners[rpcId].Contains(callback))
                {
                    rpcListeners[rpcId].Remove(callback);
                }
            }
        }

        /// <summary>
        /// Вызывает RPC функции по ID.
        /// </summary>
        public void Invoke(int rpcId, Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            if (rpcListeners.ContainsKey(rpcId))
            {
                foreach (RpcCallback callback in rpcListeners[rpcId])
                {
                    callback(player, targetId, arguments, deliveryMethod);
                }
            }
        }

        /// <summary>
        /// Получает хэш строки.
        /// </summary>
        /// <returns>Хэш строки.</returns>
        public static int GetStringHash(string value)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    hash = hash * 31 * i * 3 + c.GetHashCode();
                }
                return hash;
            }
        }
    }
}
