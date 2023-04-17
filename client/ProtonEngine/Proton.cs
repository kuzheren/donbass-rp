using LiteNetLib;
using Proton.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Proton
{
    /// <summary>
    /// Главный класс Proton, используемый из всех скриптов.
    /// </summary>
    public static class ProtonEngine
    {
        /// <summary>
        /// Объект хендлера сетевого подключения.
        /// </summary>
        private static GameObject _protonHandlerGameObject;

        /// <summary>
        /// Состояние подключения (подключен/отключен).
        /// </summary>
        public static bool IsConnected { get { bool _connected; try { _connected = ProtonNetwork.Instance != null; } catch { _connected = false; } return _connected && _protonHandlerGameObject != null; } }

        /// <summary>
        /// Состояние подключения.
        /// </summary>
        public static GameState CurrentConnectionState => ProtonGlobalStates.connectionState;

        /// <summary>
        /// Класс локального игрока.
        /// </summary>
        public static Player LocalPlayer { get; set; }

        /// <summary>
        /// Никнейм локального игрока.
        /// </summary>
        public static string NickName => LocalPlayer.nickname;

        /// <summary>
        /// Задержка между сервером и клиентом.
        /// </summary>
        public static float Latency => LocalPlayer.ping;
        //public static int Latency { get { int _latency; try { _latency = ProtonNetwork.Instance.Latency; } catch { _latency = -1; } return _latency; } }

        /// <summary>
        /// Пул игроков сервера.
        /// </summary>
        public static Dictionary<int, Player> PlayersPool => ProtonNetwork.Instance.PlayersPool;

        /// <summary>
        /// Метод для подключения к серверу.
        /// </summary>
        public static void Connect(string address, int port, string nickname)
        {
            if (IsConnected)
            {
                return;
            }

            _protonHandlerGameObject = GameObject.Instantiate(Resources.Load<GameObject>("ProtonHandler"));
            _protonHandlerGameObject.name = "ProtonHandler";

            ProtonNetwork.Instance.Connect(address, port, nickname);
        }

        /// <summary>
        /// Метод для отключения от сервера.
        /// </summary>
        public static void Disconnect()
        {
            if (ProtonNetwork.Instance == null)
            {
                return;
            }

            ProtonNetwork.Instance.Disconnect();
            GameObject.Destroy(_protonHandlerGameObject);
        }

        /// <summary>
        /// Отправляет на сервер сообщение.
        /// </summary>
        public static void SendChatMessage(string message)
        {
            if (!IsConnected)
            {
                return;
            }

            ProtonNetwork.Instance.SendChatMessage(message);
        }

        /// <summary>
        /// Отправляет RPC на сервер.
        /// </summary>
        public static void SendRPC(int rpcId, RPCTarget target, DeliveryMethod deliveryMethod, params object[] arguments)
        {
            if (!IsConnected)
            {
                return;
            }

            ProtonNetwork.Instance.SendRPC(rpcId, (int)target, deliveryMethod, arguments);
        }

        /// <summary>
        /// Отправляет RPC на сервер, но в качестве идентификатора берет хэш строки.
        /// </summary>
        public static void SendRPC(string rpcId, RPCTarget target, DeliveryMethod deliveryMethod, params object[] arguments)
        {
            if (!IsConnected)
            {
                return;
            }

            SendRPC(RpcListener.GetStringHash(rpcId), target, deliveryMethod, arguments);
        }

        /// <summary>
        /// Пытается вернуть класс игрока по его ID.
        /// </summary>
        /// <returns>Класс игрока или Null.</returns>
        public static Player GetPlayerById(int Id)
        {
            if (IsConnected == false)
            {
                return null;
            }

            if (ProtonNetwork.Instance.PlayersPool.ContainsKey(Id))
            {
                return ProtonNetwork.Instance.PlayersPool[Id];
            }

            return null;
        }
    }

    /// <summary>
    /// Класс с логикой игрока.
    /// </summary>
    public class Player
    {
        public int Id;
        public string nickname = "";
        public float ping;
        public Dictionary<string, NetworkValue> customPlayerData = new Dictionary<string, NetworkValue>();

        public Player() { }
        public Player(PlayerInfo playerInfo)
        {
            this.Id = playerInfo.Id;
            this.nickname = playerInfo.nickname;
            this.ping = playerInfo.ping;
            this.customPlayerData = playerInfo.customData;
        }
        public Player(int id, string nickname, float ping, Dictionary<string, NetworkValue> customPlayerData = null)
        {
            if (customPlayerData == null)
            {
                customPlayerData = new Dictionary<string, NetworkValue>();
            }

            this.Id = id;
            this.nickname = nickname;
            this.ping = ping;
            this.customPlayerData = customPlayerData;
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