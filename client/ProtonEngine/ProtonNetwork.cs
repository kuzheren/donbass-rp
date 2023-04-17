using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using UnityEngine;
using Proton;
using Proton.Utils;

public class ProtonNetwork : MonoBehaviour, INetEventListener
{
    /// <summary>
    /// Ключ для установки соединения между клиентом и сервером.
    /// </summary>
    public string SERVER_KEY;

    /// <summary>
    /// Главный экземпляр ProtonNetwork.
    /// </summary>
    [HideInInspector] public static ProtonNetwork Instance;

    /// <summary>
    /// Возвращает состояние подключения.
    /// </summary>
    [HideInInspector] public bool IsConnected { get { bool _connected; try { _connected = _netManager.IsRunning; } catch { _connected = false; }  return _connected && ProtonGlobalStates.connectionState == GameState.Connected; } }

    /// <summary>
    /// Возвращает задержку между сервером и клиентом в милисекундах (1000 мс в 1 сек).
    /// </summary>
    [HideInInspector] public int Latency { get { int _latency; try { _latency = _serverPeer.Ping; } catch { _latency = -1; } return _latency; } }

    /// <summary>
    /// Возвращает никнейм игрока.
    /// </summary>
    [HideInInspector] private string NickName { get; set; }

    /// <summary>
    /// Пул всех подключенных к серверу игроков.
    /// </summary>
    [HideInInspector] public Dictionary<int, Player> PlayersPool = new Dictionary<int, Player>();

    private NetManager _netManager;
    private NetPeer _serverPeer;
    private Coroutine _updateEventsCoroutine;

    /// <summary>
    /// Метод Awake, в котором создаётся главный экземпляр класса ProtonNetwork.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Метод, вызывающийся при выходе из игры и останавливающий подключение.
    /// </summary>
    private void OnApplicationQuit()
    {
        Stop(true);
    }

    /// <summary>
    /// Внутренний метод подключения к серверу.
    /// </summary>
    public void Connect(string address, int port, string nickname)
    {
        NickName = nickname;

        _netManager = new NetManager(this);
        _netManager.Start();
        _serverPeer = _netManager.Connect(address, port, SERVER_KEY);

        ProtonGlobalStates.connectionState = GameState.ConnectionRequest;

        _updateEventsCoroutine = StartCoroutine(UpdateEvents());
    }

    /// <summary>
    /// Внутренний метод отключения от сервера.
    /// </summary>
    public void Disconnect()
    {
        Stop(true);
    }

    /// <summary>
    /// Внутренний метод остановки работы клиента. Вызывается как пользователем при Disconnect(), так и сервером.
    /// </summary>
    public void Stop(bool byClient, DisconnectInfo disconnectInfo = new DisconnectInfo())
    {
        ProtonGlobalStates.connectionState = GameState.Disconnected;

        if (_serverPeer != null)
        {
            _serverPeer.Disconnect();
        }
        _netManager = null;
        _serverPeer = null;
        PlayersPool = null;
        ProtonEngine.LocalPlayer = null;

        if (!byClient)
        {
            ProtonCallbacks.Invoke_OnDisconnected(disconnectInfo);
        }

        StopCoroutine(_updateEventsCoroutine);
        GameObject.Destroy(gameObject);
    }

    /// <summary>
    /// Метод для отправки информации на сервер.
    /// </summary>
    public void SendProtonStream(ProtonStream stream, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        _serverPeer.Send(stream.Bytes.ToArray(), deliveryMethod);
    }

    /// <summary>
    /// Метод для отправки пакета на сервер.
    /// </summary>
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

    private IEnumerator UpdateEvents()
    {
        while (true)
        {
            yield return new WaitForSeconds(1 / 100.0f);
            _netManager.PollEvents();
        }
    }

    private void SendConnectionInfo()
    {
        ConnectionRequestInfo connectionInfo = new ConnectionRequestInfo(NickName, Application.version, SystemInfo.deviceUniqueIdentifier);
        ProtonPacketSerializer connectionInfoSerializer = new ProtonPacketSerializer((int)PacketId.CONNECTION_INFO, connectionInfo);

        SendPacket(connectionInfoSerializer.stream);
    }
    public void SendChatMessage(string message)
    {
        ProtonPacketSerializer chatMessageSerializer = new ProtonPacketSerializer((int)PacketId.CHAT, new object[] { message });
        SendPacket(chatMessageSerializer.stream);
    }

    public void SendRPC(int rpcId, int targetId, DeliveryMethod deliveryMethod, object[] arguments)
    {
        ProtonStream rpcPS = new ProtonStream();
        rpcPS.Write(rpcId);
        rpcPS.Write(targetId);
        rpcPS.Write(arguments.Length);

        foreach(object argument in arguments)
        {
            rpcPS.Write(new NetworkValue(argument));
        }

        SendRPCPacket(rpcPS, deliveryMethod);
    }

    public void AddPlayerToPool(Player player)
    {
        PlayersPool[player.Id] = player;
    }
    public void RemovePlayerFromPool(Player player)
    {
        if (PlayersPool.ContainsKey(player.Id))
        {
            PlayersPool.Remove(player.Id);
        }
    }

    public void OnPeerConnected(NetPeer peer)
    {
        ProtonGlobalStates.connectionState = GameState.ConnectionRequest;

        ProtonCallbacks.Invoke_OnConnected();
        SendConnectionInfo();
    }
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        try
        {
            ProtonPacketHandler.ProcessData(new ProtonStream(reader.GetRemainingBytes()), deliveryMethod);
        }
        catch (System.Exception exception)
        {
            Debug.LogError(exception);
        }
    }
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Stop(false, disconnectInfo);
    }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        ProtonCallbacks.Invoke_OnNetworkLatencyUpdated(latency);
    }
    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
    public void OnConnectionRequest(ConnectionRequest request) { }
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
}
