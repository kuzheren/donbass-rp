using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private void ProcessPlayerGetPickup(Player player, int pickupId)
        {
            PickupID pickupID = (PickupID)Enum.ToObject(typeof(PickupID), pickupId);

            switch (pickupID)
            {
                case (PickupID.Marker):
                    RemoveMarker(player);
                    DeletePickup(player, 1);
                    AddChatMessage(player, "Вы достигли точки назначения!");
                    break;

                case (PickupID.MineshaftJob): // mineshaft pickup
                    if (GetPlayerJob(player) == JobID.None)
                    {
                        CreateDialog(player, DialogID.MineshaftWorkStart);
                    }
                    else
                    {
                        //SetPlayerProperty(player, "work", null);
                        CreateDialog(player, DialogID.MineshaftWorkEnd);
                        //AddChatMessage(player, "Вы уволились с работы шахтера.");
                    }
                    break;

                case (PickupID.Frederiko): // helper pickup
                    CreateDialog(player, DialogID.FrederikoHelpChoose);
                    AddChatMessage(player, "Некоторые квесты пока в разработке!");
                    break;

                case (PickupID.RentCar): // rent car pickup
                    if (GetPlayerRentedCarId(player) != 0)
                    {
                        CreateDialog(player, DialogID.CarRentStop);
                        return;
                    }
                    CreateDialog(player, DialogID.CarRentChoose);
                    break;

                case (PickupID.BusJob):
                    if (!CheckEXPPlayerJobAbility(player, JobID.BusDriver))
                    {
                        return;
                    }

                    if (GetPlayerJob(player) == JobID.None)
                    {
                        CreateDialog(player, DialogID.BusWorkStart);
                    }
                    else
                    {
                        CreateDialog(player, DialogID.BusWorkEnd);
                    }
                    break;
            }

            if (pickupId >= 41 && pickupId <= 69)
            {
                if (busStopsPickups.Contains(pickupId))
                {
                    new Thread(() =>
                    {
                        DeletePickup(player, pickupId);
                        AddChatMessage(player, "Вы прибыли на остановку. Ожидаем 10 секунд...");
                        Thread.Sleep(10000);
                        CreateBusPickup(player, pickupId + 1);
                    }
                    ).Start();
                }
                else
                {
                    DeletePickup(player, pickupId);
                    CreateBusPickup(player, pickupId + 1);
                }
            }
            else if (pickupId == 70)
            {
                DeletePickup(player, pickupId);
                SetPlayerSalary(player, GetPlayerSalary(player) + 2000);
                AddChatMessage(player, $"Поздравляем! Вы проехали круг маршрута. Зарплата: {GetPlayerSalary(player)}");
                CreateBusPickup(player, 41);

                if (!IsQuestPassed(player, QuestID.BusDriver))
                {
                    SetQuestProgress(player, QuestID.BusDriver, GetPlayerSalary(player), 5000);
                }
            }

            if (pickupId >= 6 && pickupId <= 13)
            {
                TeleportPlayer(player, bunkerTeleportPoints[pickupId-6]);
            }
        }
    }
}
