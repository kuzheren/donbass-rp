using LiteNetLib;
using Proton.Utils;
using System.Collections;
using System.Collections.Generic;

namespace Proton
{
    /// <summary>
    /// Интерфейс, необходимый для реализации коллбэков.
    /// </summary>
    public interface IProtonCallbacks
    {
        public void OnConnected();
        public void OnDisconnected(DisconnectInfo disconnectInfo);
        public void OnInitializationFinished(ConnectionRequestAccepted requestAcceptedStructure);
        public void OnChatMessage(string message);
        public void OnNetworkLatencyUpdated(int latency);
        public void OnPlayerConnected(Player player);
        public void OnPlayerDisconnected(Player player);
        public void OnPlayersListUpdated(List<PlayerInfo> players);

    }

    public static class ProtonCallbacks
    {
        public delegate void OnConnectedEvent();
        public delegate void OnDisconnectedEvent(DisconnectInfo disconnectInfo);
        public delegate void OnInitializationFinishedEvent(ConnectionRequestAccepted requestAcceptedStructure);
        public delegate void OnChatMessageEvent(string message);
        public delegate void OnNetworkLatencyUpdatedEvent(int latency);
        public delegate void OnPlayerConnectedEvent(Player player);
        public delegate void OnPlayerDisconnectedEvent(Player player);
        public delegate void OnPlayersListUpdatedEvent(List<PlayerInfo> players);

        public static event OnConnectedEvent OnConnected;
        public static event OnDisconnectedEvent OnDisconnected;
        public static event OnInitializationFinishedEvent OnInitializationFinished;
        public static event OnChatMessageEvent OnChatMessage;
        public static event OnNetworkLatencyUpdatedEvent OnNetworkLatencyUpdated;
        public static event OnPlayerConnectedEvent OnPlayerConnected;
        public static event OnPlayerDisconnectedEvent OnPlayerDisconnected;
        public static event OnPlayersListUpdatedEvent OnPlayersListUpdated;

        private static List<IProtonCallbacks> targets = new List<IProtonCallbacks>();

        public static void AddTarget(IProtonCallbacks target)
        {
            if (!targets.Contains(target))
            {
                targets.Add(target);
                OnConnected += target.OnConnected;
                OnDisconnected += target.OnDisconnected;
                OnInitializationFinished += target.OnInitializationFinished;
                OnChatMessage += target.OnChatMessage;
                OnPlayerConnected += target.OnPlayerConnected;
                OnPlayerDisconnected += target.OnPlayerDisconnected;
                OnPlayersListUpdated += target.OnPlayersListUpdated;
            }
        }
        public static void RemoveTarget(IProtonCallbacks target)
        {
            if (targets.Contains(target))
            {
                targets.Remove(target);
                OnConnected -= target.OnConnected;
                OnDisconnected -= target.OnDisconnected;
                OnInitializationFinished -= target.OnInitializationFinished;
                OnChatMessage -= target.OnChatMessage;
                OnPlayerConnected -= target.OnPlayerConnected;
                OnPlayerDisconnected -= target.OnPlayerDisconnected;
                OnPlayersListUpdated -= target.OnPlayersListUpdated;
            }
        }

        public static void Invoke_OnConnected()
        {
            OnConnected?.Invoke();
        }
        public static void Invoke_OnDisconnected(DisconnectInfo disconnectInfo)
        {
            OnDisconnected?.Invoke(disconnectInfo);
        }
        public static void Invoke_OnInitilizationFinished(ConnectionRequestAccepted requestAcceptedStructure)
        {
            OnInitializationFinished?.Invoke(requestAcceptedStructure);
        }
        public static void Invoke_OnChatMessage(string message)
        {
            OnChatMessage?.Invoke(message);
        }
        public static void Invoke_OnNetworkLatencyUpdated(int latency)
        {
            OnNetworkLatencyUpdated?.Invoke(latency);
        }
        public static void Invoke_OnPlayerConnected(Player player)
        {
            OnPlayerConnected?.Invoke(player);
        }
        public static void Invoke_OnPlayerDisconnected(Player player)
        {
            OnPlayerDisconnected?.Invoke(player);
        }
        public static void Invoke_OnPlayersListUpdated(List<PlayerInfo> players)
        {
            OnPlayersListUpdated?.Invoke(players);
        }
    }
}