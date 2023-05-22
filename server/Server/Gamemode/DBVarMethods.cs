using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private int GetDatabaseMoney(Player player)
        {
            string money = GetDBVar(player, DBVar.Money);
            if (money == null)
            {
                return -1;
            }
            return int.Parse(money);
        }
        private void ForceSavePlayerMoneyInDatabase(Player player)
        {
            SaveDBVar(player, DBVar.Money, GetPlayerMoney(player).ToString());
        }
        private void SavePlayerMoneyInDatabase(Player player, int money)
        {
            SaveDBVar(player, DBVar.Money, (money).ToString());
        }

        private bool IsPlayerEducated(Player player)
        {
            return GetDBVar(player, DBVar.IsEducated) != null;
        }
        private void SetPlayerEducated(Player player)
        {
            SaveDBVar(player, DBVar.IsEducated, "1");
        }

        private Vector3 GetPlayerLastPosition(Player player)
        {
            string lastPosition = GetDBVar(player, DBVar.LastPosition);
            if (lastPosition == null)
            {
                return null;
            }

            try
            {
                string[] values = lastPosition.Split('(', ')', ' ');

                float x = float.Parse(values[2]);
                float y = float.Parse(values[3]);
                float z = float.Parse(values[4]);

                return new Vector3(x, y, z);
            }
            catch
            {
                return null;
            }
        }
        private void SavePlayerLastPosition(Player player, Vector3 position)
        {
            SaveDBVar(player, DBVar.LastPosition, position.ToString());
        }

        private void SaveReport(Player player, string text)
        {
            SaveDBVar(player, $"report{new Random().Next(0, 100000)}", text);
        }
    }
}
