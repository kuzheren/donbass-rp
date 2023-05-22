using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtonServer
{
    public class ServerLogic
    {
        public Server server;
        public List<Player> players = new List<Player>();

        public ServerLogic(Server server)
        {
            this.server = server;
            new Thread(UpdatePlayersInfoThread).Start();
        }

        private void UpdatePlayersInfoThread()
        {
            while (true)
            {
                Thread.Sleep(5000);
                SendPlayersInfo();
            }
        }

        public void SendPlayersInfo()
        {
            foreach (Player player in players.ToArray())
            {
                player.SendPlayersList();
            }
        }
        public void AddPlayer(Player newPlayer)
        {
            foreach (Player player in players.ToArray())
            {
                player.SendPlayerClass(newPlayer, false);
            }
            players.Add(newPlayer);
        }
        public void RemovePlayer(Player removedPlayer)
        {
            if (!players.Contains(removedPlayer))
            {
                return;
            }
            players.Remove(removedPlayer);

            foreach (Player player in players.ToArray())
            {
                player.SendRemovePlayerClass(removedPlayer);
            }
        }
        public void AddChatMessage(Player sender, string message)
        {
            foreach (Player player in players.ToArray())
            {
                player.SendChatMessage($"{sender.nickname} сказал: {message}");
            }
        }
        public void SendRPC(int rpcId, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            foreach (Player player in players.ToArray())
            {
                player.SendRPC(rpcId, deliveryMethod, arguments);
            }
        }
        public void SendRPCExcept(int rpcId, int targetId, List<NetworkValue> arguments, Player exceptedPlayer, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            foreach (Player player in players.ToArray())
            {
                if (player != exceptedPlayer)
                {
                    player.SendRPC(rpcId, deliveryMethod, arguments);
                }
            }
        }
    }
}
