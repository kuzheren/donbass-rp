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

            if (GetPlayerMoney(player) >= 10000)
            {
                GiveAchievement(player, 4);
            }
        }
        private void SavePlayerMoneyInDatabase(Player player, int money)
        {
            SaveDBVar(player, DBVar.Money, (money).ToString());

            if (money >= 10000)
            {
                GiveAchievement(player, 4);
            }
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

        private void SetPlayerAdminPermissions(Player player, bool isAdmin)
        {
            SaveDBVar(player, DBVar.IsAdmin, isAdmin ? "true" : "false");
            LoadPlayerAdminPermissions(player);
        }

        private void LoadPlayerQuestsInfo(Player player)
        {
            string questsInfo = GetDBVar(player, DBVar.QuestsInfo);

            if (questsInfo == null || questsInfo.Length < Enum.GetNames(typeof(QuestID)).Length)
            {
                questsInfo = "";
                for (int i = 0; i < Enum.GetNames(typeof(QuestID)).Length; i++)
                {
                    questsInfo += "0";
                }
            }

            for (int i = 0; i < questsInfo.Length; i++)
            {
                if (questsInfo[i] == '1')
                {
                    QuestID questId = (QuestID)Enum.GetValues(typeof(QuestID)).GetValue(i);
                    SetQuestPassed(player, questId);
                }
            }
        }
        private void ForceSavePlayerQuestsInfo(Player player)
        {
            string questsInfo = "";

            foreach (QuestID questId in Enum.GetValues(typeof(QuestID)))
            {
                questsInfo += IsQuestPassed(player, questId) ? "1" : "0";
            }

            SaveDBVar(player, DBVar.QuestsInfo, questsInfo);
        }

        private int GetDatabaseEXP(Player player)
        {
            string EXP = GetDBVar(player, DBVar.EXP);
            if (EXP == null)
            {
                return -1;
            }
            return int.Parse(EXP);
        }
        private void ForceSavePlayerEXPInDatabase(Player player)
        {
            SaveDBVar(player, DBVar.EXP, GetPlayerEXP(player).ToString());
        }
        private void SavePlayerEXPInDatabase(Player player, int EXP)
        {
            SaveDBVar(player, DBVar.EXP, (EXP).ToString());
        }
    }
}
