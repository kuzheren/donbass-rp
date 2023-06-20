// Donbass RP gamemode by kuzheren
// Version - beta
// 2023

using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public static class RandomExtensions
    {
        public static float NextSingle(this Random random, float minValue, float maxValue)
        {
            return random.NextSingle() * (maxValue - minValue) + minValue;
        }
    }

    public partial class Gamemode
    {
        private void ProcessPlayerJoinedEvent(Player player)
        {
            AddGlobalChatMessage($"В игру зашел {player.nickname}");

            if (IsPlayerLogged(player))
            {
                int databaseMoney = GetDatabaseMoney(player);
                if (databaseMoney == -1)
                {
                    databaseMoney = 100;
                    SavePlayerMoneyInDatabase(player, databaseMoney);
                }
                SetPlayerMoney(player, databaseMoney);

                int databaseEXP = GetDatabaseEXP(player);
                if (databaseEXP == -1)
                {
                    databaseEXP = 1;
                    SavePlayerEXPInDatabase(player, databaseEXP);
                }
                SetPlayerEXP(player, databaseEXP);

                LoadPlayerAdminPermissions(player);
                LoadPlayerQuestsInfo(player);
                LoadPlayerDatabaseValues(player);
            }
            else
            {
                SetPlayerMoney(player, 100);
            }

            if (GetPlayerMoney(player) >= 10000)
            {
                GiveAchievement(player, 4);
            }

            GiveAchievement(player, 0);
            AddChatMessage(player, $"Добро пожаловать, {player.nickname}! Версия сервера: \"{Config.SERVER_VERSION}\"");

            if (GetBombingState())
            {
                PlayBombingAlertAudio(player);
            }
            else
            {
                PlayGameThemeAudio(player);
            }

            InitBulletHoles(player);
            InitDoors(player);
            InitMapping(player);
            InitTextdraws(player);
            InitPickups(player);

            SetPlayerHealth(player, 100f, false);
            SetBombingLivePlayerState(player, GetBombingState());
            SetPlayerJob(player, JobID.None);
            ShowBannerAd(player);

            if (IsAdmin(player))
            {
                foreach (Player serverPlayer in PlayersList)
                {
                    GiveAchievement(serverPlayer, 9);
                }
            }
            else
            {
                foreach (Player serverPlayer in PlayersList)
                {
                    if (IsAdmin(serverPlayer))
                    {
                        GiveAchievement(player, 9);
                    }
                }
            }

            new Thread(new ParameterizedThreadStart(CreatePlayerMarkers)).Start(player);

            if (!IsPlayerEducated(player))
            {
                new Thread(new ParameterizedThreadStart(StartTutorial)).Start(player);
            }
            else
            {
                CreateDialog(player, DialogID.SpawnChoose);
            }
        }
        private void ProcessPlayerDisconnectEvent(Player player, DisconnectInfo disconnectInfo)
        {
            if (player == null)
            {
                return;
            }

            AddGlobalChatMessage($"Игрок {player.nickname} вышел из игры.");

            foreach (Player serverPlayer in PlayersList)
            {
                DeleteTextdraw(serverPlayer, 100 + player.Id);
            }

            if (IsPlayerLogged(player))
            {
                if (GetPlayerSalary(player) != 0)
                {
                    SetPlayerMoney(player, GetPlayerMoney(player) + GetPlayerSalary(player));
                }

                ForceSavePlayerMoneyInDatabase(player);
                ForceSavePlayerQuestsInfo(player);
                ForceSavePlayerEXPInDatabase(player);

                if (GetPlayerPosition(player) == null)
                {
                    return;
                }
                SavePlayerLastPosition(player, GetPlayerPosition(player));
            }
        }
        private void ProcessPlayerDeathEvent(Player player)
        {
            SetBombingLivePlayerState(player, false);
            if (GetBombingState() && !IsQuestPassed(player, QuestID.BombAliver))
            {
                AddChatMessage(player, "К сожалению, вы погибли во время бомбардировки.");
                AddChatMessage(player, "В этот раз вам не удалось выполнить квест \"Опять в бункер\".");
                AddChatMessage(player, "Удачи в следующий раз!");
            }

            RespawnPlayer(player);
        }
        private void ProcessClickEntityEvent(Player player, int entityId)
        {
            if (entityId >= 1 && entityId <= 100000) // mineshaft
            {
                string oreName = "";

                List<int> minechaftObjectsIds = GetPlayerMineshaftOres(player);

                if (minechaftObjectsIds.Contains(entityId))
                {
                    minechaftObjectsIds.Remove(entityId);
                }
                else
                {
                    return;
                }

                if (entityId >= 1 && entityId <= 33333) // golden ore
                {
                    SetPlayerSalary(player, GetPlayerSalary(player) + 20);
                    oreName = "Золотая руда";
                }
                else if (entityId >= 33334 && entityId <= 100000) // coal ore
                {
                    SetPlayerSalary(player, GetPlayerSalary(player) + 1);
                    oreName = "Угольная руда";
                }

                AddChatMessage(player, $"Вы собрали: {oreName}. Ваша зарплата: {GetPlayerSalary(player)}");

                if (!IsQuestPassed(player, QuestID.Mineshafter))
                {
                    SetQuestProgress(player, QuestID.Mineshafter, GetQuestProgress(player, QuestID.Mineshafter) + 1, 100);
                }

                DestroyObject(player, entityId);

                (int objectId, string spawnedOreName, Vector3 position) = CreateRandomOre(player);

                minechaftObjectsIds.Add(objectId);
                SetPlayerMineshaftOres(player, minechaftObjectsIds);
            }
            else if (entityId == 100006)
            {
                if (GetPlayerHealth(player) >= 100f)
                {
                    return;
                }

                SetPlayerHealth(player, GetPlayerHealth(player) + 1, true);
            }
        }
        private void ProcessClickPlayerEvent(Player player, Player targetPlayer)
        {
            if (targetPlayer == null)
            {
                return;
            }

            if (!IsAdmin(player))
            {
                return;
            }

            SetAdminActionsTargetPlayer(player, targetPlayer);
            CreateDialog(player, DialogID.AdminPlayerMenu);
        }
        private void ProcessPlayerEducatedEvent(Player player)
        {
            AddChatMessage(player, "Хорошо, теперь выбери пункт \"Бомбецк\".");
            CreateDialog(player, DialogID.SpawnChoose);
        }
        private void ProcessOpenDoorEvent(Player player, int doorId, bool open)
        {
            if (open)
            {
                if (!openedDoors.Contains(doorId))
                {
                    openedDoors.Add(doorId);
                }
            }
            else
            {
                if (openedDoors.Contains(doorId))
                {
                    openedDoors.Remove(doorId);
                }
            }

            if (doorId == -1098202634 && open) // hofman house
            {
                GiveAchievement(player, 5);
                if (!IsQuestPassed(player, QuestID.Hofman))
                {
                    SetQuestProgress(player, QuestID.Hofman, 1, 1);
                }
            }
        }
        private void ProcessPlayerEnterTransport(Player player, int Id)
        {
        }
        private void ProcessPlayerQuitTransport(Player player, int Id)
        {
        }
        private void ProcessPromoCode(Player player, string promoCode)
        {
        }
        private void ProcessQuestCompletedEvent(Player player, QuestID questId)
        {
            SetQuestPassed(player, questId);

            switch (questId)
            {
                case (QuestID.Mineshafter):
                    {
                        SetPlayerMoney(player, GetPlayerMoney(player) + 1000);
                        SetPlayerEXP(player, GetPlayerEXP(player) + 1);

                        ForceSavePlayerMoneyInDatabase(player);
                        ForceSavePlayerEXPInDatabase(player);
                        break;
                    }

                case (QuestID.Hofman):
                    {
                        SetPlayerMoney(player, GetPlayerMoney(player) + 3000);

                        ForceSavePlayerMoneyInDatabase(player);
                        break;
                    }

                case (QuestID.BombAliver):
                    {
                        SetPlayerEXP(player, GetPlayerEXP(player) + 2);

                        ForceSavePlayerEXPInDatabase(player);
                        break;
                    }

                case (QuestID.BusDriver):
                    {
                        SetPlayerMoney(player, GetPlayerMoney(player) + 2000);
                        SetPlayerEXP(player, GetPlayerEXP(player) + 1);

                        ForceSavePlayerMoneyInDatabase(player);
                        ForceSavePlayerEXPInDatabase(player);
                        break;
                    }
            }

            ForceSavePlayerQuestsInfo(player);
        }
    }
}
