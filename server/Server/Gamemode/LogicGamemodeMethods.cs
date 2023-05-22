using LiteNetLib;
using System.Net;
using System.Net.Sockets;

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

            AddChatMessage(player, $"Вы выбрали маршурт {wayId}");
            AddChatMessage(player, "Отправляйтесь на первую метку. Она отмечена на карте красным цветом.");
            InstantiatePhotonBundleObject(player, busId, "veh_Bus", new Vector3(394.3f, -7.9f, -538), new Vector3(0, 30, 0));

            switch (wayId)
            {
                case (0):
                    {
                        CreateBusPickup(player, 41);
                        break;
                    }
            }
        }
    }
}
