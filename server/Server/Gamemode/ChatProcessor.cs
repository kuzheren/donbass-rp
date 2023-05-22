using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        public void OnChatMessage(Player player, string message)
        {
            ProcessChatMessage(player, message);
        }

        private void ProcessChatMessage(Player player, string message)
        {
            if (!IsPlayerLogged(player))
            {
                AddChatMessage(player, "Войдите в аккаунт, чтобы отправлять сообщения в чат.");
                return;
            }

            AddGlobalChatMessage($"{player.nickname} ({player.Id}): {message}");
            Utils.WriteLog($"{DateTime.Now.ToString("[HH:mm:ss]")} {player.nickname} ({player.Id}): {message}", "chat.txt");
        }
    }
}
