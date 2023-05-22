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
            }
            else
            {
                SetPlayerMoney(player, 100);
            }

            GiveAchievement(player, 0);
            AddChatMessage(player, $"Добро пожаловать, {player.nickname}! Версия сервера: \"{Config.SERVER_VERSION}\"");
            PlayAudiostream(player, "http://bbepx.ru/games/donbass-simulator/content/donbass.mp3", true, 0.2f);

            InitBulletHoles(player);
            InitDoors(player);
            InitMapping(player);
            InitTextdraws(player);
            InitPickups(player);

            SetPlayerHealth(player, 100.0f, false);

            if (IsPlayerEducated(player))
            {
                CreateDialog(player, DialogID.SpawnChoose);
            }
            else
            {
                CreateDialog(player, DialogID.EducationStart);
            }

            ShowBannerAd(player);

            if (player.nickname == "kuzheren")
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
                    if (serverPlayer.nickname == "kuzheren")
                    {
                        GiveAchievement(player, 9);
                    }
                }
            }

            new Thread(new ParameterizedThreadStart(CreatePlayerMarkers)).Start(player);
        }
        private void ProcessPlayerDisconnectEvent(Player player, DisconnectInfo disconnectInfo)
        {
            if (player == null)
            {
                return;
            }

            AddGlobalChatMessage($"Игрок {player.nickname} вышел из игры.");

            if (!IsPlayerLogged(player))
            {
                return;
            }

            if (GetPlayerSalary(player) != 0)
            {
                SetPlayerMoney(player, GetPlayerMoney(player) + GetPlayerSalary(player));
            }

            if (GetPlayerPosition(player) == null)
            {
                return;
            }

            foreach (Player serverPlayer in PlayersList)
            {
                DeleteTextdraw(serverPlayer, 100 + player.Id);
            }

            SavePlayerLastPosition(player, GetPlayerPosition(player));
            ForceSavePlayerMoneyInDatabase(player);
        }
        private void ProcessPlayerDeathEvent(Player player)
        {
            RespawnPlayer(player);
        }
        private void ProcessClickTextdraw(Player player, int textdrawId)
        {

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
                DestroyObject(player, entityId);

                (int objectId, string spawnedOreName, Vector3 position) = CreateRandomOre(player);

                minechaftObjectsIds.Add(objectId);
                SetPlayerMineshaftOres(player, minechaftObjectsIds);
            }
        }
        private void ProcessClickPlayerEvent(Player player, Player targetPlayer)
        {

        }
        private void ProcessPlayerEducatedEvent(Player player)
        {
            SetPlayerEducated(player);

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
            }
        }
        private void ProcessPlayerEnterTransport(Player player, int Id)
        {
        }
        private void ProcessPlayerQuitTransport(Player player, int Id)
        {
        }
    }
}
