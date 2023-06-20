using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private void LoadPlayerDatabaseValues(Player player)
        {
            Dictionary<string, string> result = ExecuteQuery($"SELECT id, register_date FROM players WHERE token = \'{GetPlayerToken(player)}\'");

            int registerTime = int.Parse(result["register_date"]);
            int accountId = int.Parse(result["id"]);

            SetPlayerRegisterDate(player, registerTime);
            SetPlayerRegisterId(player, accountId);
        }
    }
}
