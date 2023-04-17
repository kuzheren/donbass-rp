using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Proton.Utils
{
    public static class ProtonPacketHandler
    {
        public static void ProcessData(ProtonStream data, DeliveryMethod deliveryMethod)
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
        private static void ProcessPacket(ProtonStream data, Dictionary<string, NetworkValue> customPacketData, DeliveryMethod deliveryMethod)
        {
            int packetId = data.Read<int>();
            PacketId packet = (PacketId)Enum.ToObject(typeof(PacketId), packetId);

            switch (packet)
            {
                case (PacketId.CONNECTION_ACCEPTED):
                    ProtonPacketDeserializer requestAcceptedSerializer = new ProtonPacketDeserializer(data, typeof(ConnectionRequestAccepted));
                    ConnectionRequestAccepted requestAcceptedStructure = (ConnectionRequestAccepted)requestAcceptedSerializer.structure;

                    ProtonGlobalStates.connectionState = GameState.Connected;

                    ProtonCallbacks.Invoke_OnInitilizationFinished(requestAcceptedStructure);
                    break;

                case (PacketId.PLAYERS_LIST):
                    NetworkValue playersListValue = data.Read<NetworkValue>();
                    List<NetworkValue> playersList = (List<NetworkValue>)playersListValue.value;
                    List<PlayerInfo> players = new List<PlayerInfo>();

                    foreach (NetworkValue player in playersList)
                    {
                        NetworkStructure structure = (NetworkStructure)player.value;
                        IProtonSerializable serializableStructure = structure.structure;
                        PlayerInfo listPlayerInfo = (PlayerInfo)serializableStructure;
                        players.Add(listPlayerInfo);
                    }

                    foreach (PlayerInfo player in players)
                    {
                        ProtonNetwork.Instance.PlayersPool[player.Id] = new Player(player.Id, player.nickname, player.ping, player.customData);
                    }

                    ProtonCallbacks.Invoke_OnPlayersListUpdated(players);
                    break;

                case (PacketId.PLAYER_CLASS):
                    bool local = (bool)customPacketData["local"].value;
                    ProtonPacketDeserializer playerInfoDeserializer = new ProtonPacketDeserializer(data, typeof(PlayerInfo));
                    PlayerInfo newPlayerInfo = (PlayerInfo)playerInfoDeserializer.structure;
                    Player connectedPlayer = new Player(newPlayerInfo);

                    if (local)
                    {
                        ProtonEngine.LocalPlayer = new Player(newPlayerInfo);
                        ProtonNetwork.Instance.AddPlayerToPool(connectedPlayer);
                    }
                    else
                    {
                        ProtonNetwork.Instance.AddPlayerToPool(connectedPlayer);
                        ProtonCallbacks.Invoke_OnPlayerConnected(connectedPlayer);
                    }
                    break;

                case (PacketId.PLAYER_CLASS_REMOVE):
                    int removedPlayerId = data.Read<int>();
                    Player removedPlayer = ProtonEngine.GetPlayerById(removedPlayerId);

                    if (removedPlayer != null)
                    {
                        ProtonNetwork.Instance.RemovePlayerFromPool(removedPlayer);
                        ProtonCallbacks.Invoke_OnPlayerDisconnected(removedPlayer);
                    }
                    break;

                case (PacketId.CHAT):
                    string message = data.Read<string>();
                    ProtonCallbacks.Invoke_OnChatMessage(message);
                    break;
            }
        }
        private static void ProcessRPC(ProtonStream data, DeliveryMethod deliveryMethod)
        {
            int rpcId = data.Read<int>();
            int argumentsLength = data.Read<int>();

            List<NetworkValue> arguments = new List<NetworkValue>();
            for (int i = 0; i < argumentsLength; i++)
            {
                arguments.Add(data.Read<NetworkValue>());
            }

            RpcListener.Invoke(rpcId, arguments, deliveryMethod);
        }
    }
}