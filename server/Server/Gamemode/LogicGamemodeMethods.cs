using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private string GetPlayerMoneyText(Player player)
        {
            int money = GetPlayerMoney(player);

            return money.ToString();

            //if (money < 1000)
            //{
            //    return money.ToString();
            //}
            //else if (money < 1000000)
            //{
            //    return Math.Round((float)money / 1000, 1).ToString() + "К";
            //}
            //else if (money < 1000000000)
            //{
            //    return Math.Round((float)money / 1000000, 1).ToString() + "М";
            //}
            //else
            //{
            //    return Math.Round((float)money / 1000000000, 1).ToString() + "МЛРД";
            //}
        }
        private void UpdateMoneyPanel(Player player)
        {
            CreateTextdraw(player, 1, "money", false, new Vector2(150, -451.21f), new Vector2(300, 100), new Vector2(0, 1), new Vector2(0, 1), $"Деньги: {GetPlayerMoneyText(player)}");
        }
        private void CreateMarker(Player player, Vector3 position)
        {
            CreateTextdraw(player, 6, "ptr", false, ConvertWorldPositionToMap(position), new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), onMap: true);
            CreatePickup(player, position, 1, true);
            AddChatMessage(player, "Метка отмечена на карте");
        }
        private void RemoveMarker(Player player)
        {
            DeleteTextdraw(player, 6);
        }

        private (int, string, Vector3) CreateRandomOre(Player player)
        {
            int chanceNumber = new Random().Next(0, 100);
            string spawnedOreName = "";
            int objectId = 0;
            Vector3 position = new Vector3(-468.4f, 7.09f, new Random().Next(650, 700));

            if (chanceNumber <= 3)
            {
                spawnedOreName = "mGold";
                objectId = new Random().Next(1, 33333);
            }
            else if (chanceNumber >= 4 && chanceNumber <= 100)
            {
                spawnedOreName = "mCoal";
                objectId = new Random().Next(33334, 100000);
            }

            CreateObject(player, spawnedOreName, position, objectId: objectId);

            return (objectId, spawnedOreName, position);
        }
        private void CreateMineshaftObjects(Player player)
        {
            List<int> minechaftObjectsIds = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                (int objectId, string spawnedOreName, Vector3 position) = CreateRandomOre(player);

                minechaftObjectsIds.Add(objectId);
                SetPlayerMineshaftOres(player, minechaftObjectsIds);
            }
        }
        private void DeleteMineshaftObjects(Player player)
        {
            List<int> minechaftObjectsIds = GetPlayerMineshaftOres(player);

            foreach (int i in minechaftObjectsIds)
            {
                DestroyObject(player, i);
            }
        }


        private void CreatePlayerMarkers(object threadArg)
        {
            Player player = (Player)threadArg;

            while (true)
            {
                Thread.Sleep(3000);

                foreach (Player serverPlayer in PlayersList)
                {
                    Vector3 position = GetPlayerPosition(serverPlayer);
                    if (position == null || serverPlayer == player)
                    {
                        continue;
                    }

                    Vector2 mapPosition = ConvertWorldPositionToMap(position);

                    CreateTextdraw(player, 100 + serverPlayer.Id, "pos", false, mapPosition, new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), onMap: true);
                }
            }
        }

        private void ProcessHelpRequest(Player player, int listItemId)
        {
            if (listItemId >= helpResponses.Count)
            {
                return;
            }

            CreateDialog(player, DialogID.FrederikoHelpInfo, helpResponses[listItemId]);
        }

        private void GiveAmmoSynced(Player player, WeaponEnum weaponType, int ammount) // called by actions
        {
            GiveAmmo(player, weaponType, ammount);
            StorePlayerAmmo(player, weaponType, GetPlayerAmmo(player, weaponType) + ammount);
        }
        private void RemoveAmmoSynced(Player player, WeaponEnum weaponType, int ammount) // called by actions
        {
            RemoveAmmo(player, weaponType, ammount);
            StorePlayerAmmo(player, weaponType, GetPlayerAmmo(player, weaponType) - ammount);
        }

        private void GivePVPWeapons(Player player)
        {
            GiveAmmoSynced(player, WeaponEnum.UZY, 300);
            GiveAmmoSynced(player, WeaponEnum.AK78, 200);
            GiveAmmoSynced(player, WeaponEnum.AVP, 30);
            GiveAmmoSynced(player, WeaponEnum.Ashotgun, 50);
        }

        private void CreateBusPickup(Player player, int Id)
        {
            CreatePickup(player, Id, true, true);
            CreateTextdraw(player, 6, "ptr", false, ConvertWorldPositionToMap(pickupInfo[Id]), new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), onMap: true);
        }
        private void StartBusJob(Player player, int wayId)
        {
            int busId = new Random().Next(200001, 250000);
            SetPlayerJobCarId(player, busId);

            AddChatMessage(player, "Отправляйтесь на первую метку. Она отмечена на карте красным цветом.");
            InstantiatePhotonBundleObject(player, busId, "veh_Bus", new Vector3(394.3f, -7.9f, -538), new Vector3(0, -150, 0));

            switch (wayId)
            {
                case (0):
                    {
                        CreateBusPickup(player, 41);
                        break;
                    }
            }
        }

        private void CreateQuestDialog(Player player, int questId)
        {
            QuestID questIdEnum = (QuestID)Enum.ToObject(typeof(DialogID), questId);
            bool isQuestPassed = IsQuestPassed(player, questIdEnum);
            string questInfoValue = questInfo.ElementAt(questId).Value;

            string questReward = $"<color=orange>{questInfoValue.Split(';')[0]}</color>";
            string questDescription = questInfoValue.Split(';')[1];

            CreateDialog(player, DialogID.QuestInfo, questInfo.ElementAt(questId).Key, isQuestPassed ? "<color=green>Да</color>" : "<color=red>Нет</color>", questReward, questDescription);

            if (!IsPlayerEducated(player) && questIdEnum == QuestID.Mineshafter)
            {
                SetPlayerEducated(player);

                AddChatMessage(player, "==Отлично, ты взял квест!==");
                AddChatMessage(player, "Отправляйся на работу шахтёра, чтобы выполнить его");
                AddChatMessage(player, "==Арендуй машину с помощью <color=red>Метки</color>==");
            }
        }

        private bool CheckEXPPlayerJobAbility(Player player, JobID jobId)
        {
            if (GetPlayerEXP(player) >= (int)jobId)
            {
                return true;
            }

            AddChatMessage(player, "==Вы не можете отправиться на эту работу==");
            AddChatMessage(player, $"Ваш уровень: {GetPlayerEXP(player)}. Требуемый для работы уровень: {(int)jobId}");
            AddChatMessage(player, "==Выполняйте квесты для повышения уровня==");

            return false;
        }

        private void CreateErrorTraceTextdraw(Player player, Exception exception)
        {
            CreateTextdraw(player, -1, "exception", true, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), $"На сервере произошла ошибка.\nИмя: {exception.GetType().Name}\nСообщение: {exception.Message}\nПодробная информация:\n{exception}\n<color=green>НАЖМИТЕ НА ЭКРАН ЧТОБЫ СКРЫТЬ ТЕКСТ ОШИБКИ!</color>", resizeText: true);
        }

        private void StartTutorial(object playerObject)
        {
            Player player = (Player)playerObject;
            PlayAudiostream(player, "rus.mp3", volume: 0.3f);

            Thread.Sleep(1000);

            AddChatMessage(player, "DONBASS SIMULATOR LOADED!");

            Thread.Sleep(2000);

            CreateDialog(player, DialogID.HelloMessageDialog);

            Thread.Sleep(3000);

            CreateDialog(player, DialogID.PromoCodeDialog);
        }

        private void PlayBombingAlertAudio(Player player)
        {
            PlayAudiostream(player, "yaderka.mp3", volume: 0.2f);
        }
        private void PlayGameThemeAudio(Player player)
        {
            PlayAudiostream(player, "donbass.mp3", isLoop:true, volume: 0.1f);
        }

        private void BombingThread()
        {
            while (true)
            {
                Thread.Sleep(new Random().Next(1000 * 60 * 20, 1000 * 60 * 25));
                StartBombingEvent();
            }
        }
        private void StartBombingEvent()
        {
            SetBombingState(true);

            PlayGlobalAudiostream("yaderka.mp3", volume: 0.2f);

            AddGlobalChatMessage("==ВНИМАНИЕ! ВОЗДУШНАЯ ТРЕВОГА!==");
            AddGlobalChatMessage("Срочно отыщите укрытие на время бомбардировки!");
            AddGlobalChatMessage("Если вы находитесь в здании, выбегайте оттуда в подвал!");
            AddGlobalChatMessage("Найти ближайший подвал можно при помощи команды /bunker");
            AddGlobalChatMessage("==ПЕРЕЖИДАЙТЕ БОМБАРДИРОВКУ В УКРЫТИИ!==");

            foreach (Player player in PlayersList)
            {
                SetBombingLivePlayerState(player, true);
            }
            
            new Thread(SpawnBombingPlanes).Start();
        }
        private void SpawnBombingPlanes()
        {
            Utils.Log("Waiting for spawn of planes...");

            Thread.Sleep(2000);

            AddGlobalChatMessage("ДО БОМБАРДИРОВКИ 40 СЕКУНД!");
            AddGlobalChatMessage("ИЩИ УКРЫТИЕ! ВВЕДИ /bunker");

            Thread.Sleep(40000);

            Utils.Log("Spawn of planes has begun!");

            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(3000);
                for (int j = 0; j < 10; j++)
                {
                    Thread.Sleep(500);
                    SpawnRandomBombingPlane();
                }
            }

            Utils.Log("Spawn of planes has stopped!");

            Thread.Sleep(5000);

            AddGlobalChatMessage("Бомбежка окончена. Можно выходить из укрытий");
            SetBombingState(false);
            PlayGlobalAudiostream("donbass.mp3", isLoop: true, volume: 0.1f);

            foreach (Player player in PlayersList)
            {
                AddChatMessage(player, "Поздравляем! Вы пережили бомбардировку!");
                if (GetBombingLivePlayerState(player) && !IsQuestPassed(player, QuestID.BombAliver))
                {
                    SetQuestProgress(player, QuestID.BombAliver, 1, 1);
                }
                else if (GetBombingLivePlayerState(player))
                {
                    AddChatMessage(player, "Вы получаете 1 EXP за выживание при бомбардировке!");
                    SetPlayerEXP(player, GetPlayerEXP(player) + 1);
                    ForceSavePlayerEXPInDatabase(player);
                }
            }
        }

        private void CreateClosestBunkerPickup(Player player)
        {
            Vector3 playerPosition = GetPlayerPosition(player);
            Vector3 closestPoint = bunkerEnterPickups[0];
            float closestDistance = Vector3.Distance(playerPosition, closestPoint);

            for (int i = 1; i < bunkerEnterPickups.Count; i++)
            {
                float distance = Vector3.Distance(playerPosition, bunkerEnterPickups[i]);
                if (distance < closestDistance)
                {
                    closestPoint = bunkerEnterPickups[i];
                    closestDistance = distance;
                }
            }

            CreateMarker(player, closestPoint + new Vector3(0, 35, 0));
        }
    }
}
