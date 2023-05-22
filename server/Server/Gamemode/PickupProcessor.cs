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
                    if (GetPlayerJob(player) == null)
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
                    CreateDialog(player, DialogID.FrederikoHelpRequestDialog);
                    break;

                case (PickupID.QuestHelper): // quest pickup
                    AddChatMessage(player, "Еще недоступно :(");
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
                    if (GetPlayerJob(player) == null)
                    {
                        CreateDialog(player, DialogID.BusWorkStart);
                    }
                    else
                    {
                        //SetPlayerProperty(player, "work", null);
                        CreateDialog(player, DialogID.BusWorkEnd);
                        //AddChatMessage(player, "Вы уволились с работы шахтера.");
                    }
                    break;
            }

            if (pickupId >= 41 && pickupId <= 56)
            {
                DeletePickup(player, pickupId);
                CreateBusPickup(player, pickupId + 1);
            }
            else if (pickupId == 57)
            {
                DeletePickup(player, pickupId);
                SetPlayerSalary(player, GetPlayerSalary(player) + 2000);
                AddChatMessage(player, $"Поздравляем! Вы проехали весь маршрут. Зарплата: {GetPlayerSalary(player)}");
                CreateBusPickup(player, 41);
            }
        }
    }
}
