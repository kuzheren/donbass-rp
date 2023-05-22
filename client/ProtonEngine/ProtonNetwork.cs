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
    /// ���� ��� ��������� ���������� ����� �������� � ��������.
    /// </summary>
    public string SERVER_KEY;

    /// <summary>
    /// ������� ��������� ProtonNetwork.
    /// </summary>
    [HideInInspector] public static ProtonNetwork Instance;

    /// <summary>
    /// ���������� ��������� �����������.
    /// </summary>
    [HideInInspector] public bool IsConnected { get { bool _connected; try { _connected = _netManager.IsRunning; } catch { _connected = false; }  return _connected && ProtonGlobalStates.connectionState == GameState.Connected; } }

    /// <summary>
    /// ���������� �������� ����� �������� � �������� � ������������ (1000 �� � 1 ���).
    /// </summary>
    [HideInInspector] public int Latency { get { int _latency; try { _latency = _serverPeer.Ping; } catch { _latency = -1; } return _latency; } }

    /// <summary>
    /// ���������� ������� ������.
    /// </summary>
    [HideInInspector] private string NickName { get; set; }

    /// <summary>
    /// ��� ���� ������������ � ������� �������.
    /// </summary>
    [HideInInspector] public Dictionary<int, Player> PlayersPool = new Dictionary<int, Player>();

    private NetManager _netManager;
    private NetPeer _serverPeer;
    private Coroutine _updateEventsCoroutine;
    private Dictionary<string, NetworkValue> customLoginInfo;

    /// <summary>
    /// ����� Awake, � ������� �������� ������� ��������� ������ ProtonNetwork.
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
    /// �����, ������������ ��� ������ �� ���� � ��������������� �����������.
    /// </summary>
    private void OnApplicationQuit()
    {
        Stop(true);
    }

    /// <summary>
    /// ���������� ����� ����������� � �������.
    /// </summary>
    public void Connect(string address, int port, string nickname, Dictionary<string, NetworkValue> customLoginInfo)
    {
        NickName = nickname;
        this.customLoginInfo = customLoginInfo;

        _netManager = new NetManager(this);
        _netManager.DisconnectTimeout = 20000;
        _netManager.Start();
        _serverPeer = _netManager.Connect(address, port, SERVER_KEY);

        ProtonGlobalStates.connectionState = GameState.ConnectionRequest;

        _updateEventsCoroutine = StartCoroutine(UpdateEvents());
    }

    /// <summary>
    /// ���������� ����� ���������� �� �������.
    /// </summary>
    public void Disconnect()
    {
        Stop(true);
    }

    /// <summary>
    /// ���������� ����� ��������� ������ �������. ���������� ��� ������������� ��� Disconnect(), ��� � ��������.
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
    /// ����� ��� �������� ���������� �� ������.
    /// </summary>
    public void SendProtonStream(ProtonStream stream, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        _serverPeer.Send(stream.Bytes.ToArray(), deliveryMethod);
    }

    /// <summary>
    /// ����� ��� �������� ������ �� ������.
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
            yield return new WaitForSeconds(1 / 1000.0f);
            _netManager.PollEvents();
        }
    }

    private void SendConnectionInfo()
    {
        ConnectionRequestInfo connectionInfo = new ConnectionRequestInfo(NickName, Application.version, SystemInfo.deviceUniqueIdentifier, customLoginInfo);
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
