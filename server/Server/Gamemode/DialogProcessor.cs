using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private void ProcessDialogResponse(Player player, int dialogId, int listItemId, bool cancel, string responseText, DialogID dialogEnum)
        {
            if (dialogId > 9 && dialogId < 19) // education
            {
                if (cancel)
                {
                    CreateDialog(player, dialogId - 1);
                    return;
                }
                CreateDialog(player, dialogId + 1);
                return;
            }

            switch (dialogEnum)
            {
                case (DialogID.SpawnChoose):
                    if (cancel)
                    {
                        CreateDialog(player, DialogID.SpawnChoose);
                        return;
                    }

                    switch (listItemId)
                    {
                        case (0):
                            SpawnPlayer(player, new Vector3(650, -9, 608));
                            break;

                        case (1):
                            if (!IsPlayerLogged(player))
                            {
                                AddChatMessage(player, "У вас нет последней точки выхода.");
                                CreateDialog(player, DialogID.SpawnChoose);
                                return;
                            }

                            Vector3 spawnPosition = GetPlayerLastPosition(player);
                            if (spawnPosition == null)
                            {
                                AddChatMessage(player, "У вас нет последней точки выхода, либо она загрузилась с ошибкой.");
                                CreateDialog(player, DialogID.SpawnChoose);
                                return;
                            }

                            SpawnPlayer(player, spawnPosition);
                            break;
                    }
                    break;

                case (DialogID.EducationStart): // education start
                    if (cancel)
                    {
                        CreateDialog(player, DialogID.EducationStart);
                        return;
                    }
                    CreateDialog(player, dialogId + 1);
                    break;

                case (DialogID.EducationEnd): // education end
                    if (cancel)
                    {
                        CreateDialog(player, dialogId - 1);
                        return;
                    }
                    ProcessPlayerEducatedEvent(player);
                    break;

                case (DialogID.Report): // report
                    if (cancel || responseText.Trim() == "")
                    {
                        return;
                    }
                    SaveReport(player, $"{player.nickname}: {responseText}");
                    AddChatMessage(player, $"Вы отправили разработчикам отзыв: {responseText}. Спасибо!");
                    break;

                case (DialogID.Help): // help dialog
                    if (cancel)
                    {
                        return;
                    }

                    switch (listItemId) // "Информация об аккаунте", "Список команд", "Обращение к администрации", "Обучение", "Работы"
                    {
                        case (0):
                            CreateDialog(player, DialogID.AccountInfo, player.nickname, "неизвестно", "неизвестно", GetPlayerMoney(player));
                            break;

                        case (1):
                            CreateDialog(player, DialogID.CMDList);
                            break;

                        case (2):
                            CreateDialog(player, DialogID.Report);
                            break;

                        case (3):
                            DeletePlayer(player);
                            CreateDialog(player, DialogID.EducationStart);
                            break;

                        case (4):
                            CreateDialog(player, DialogID.JobSelector);
                            break;
                    }
                    break;

                case (DialogID.CMDList): // cmd list
                    CreateDialog(player, DialogID.Help);
                    break;

                case (DialogID.AccountInfo): // account info
                    CreateDialog(player, DialogID.Help);
                    break;

                case (DialogID.JobSelector): // jobs
                    if (cancel)
                    {
                        return;
                    }

                    switch (listItemId)
                    {
                        case (0):
                            CreateMarker(player, new Vector3(-404.33f, 27.3f, 640.9116f));
                            break;

                        case (1):
                            CreateMarker(player, new Vector3(408.1f, 28, -547));
                            break;

                        default:
                            AddChatMessage(player, "Эта работа в процессе разработки.");
                            break;
                    }
                    break;

                case (DialogID.MineshaftWorkStart): // mineshaft job
                    if (cancel)
                    {
                        return;
                    }

                    SetPlayerJob(player, "mineshaft");
                    AddChatMessage(player, "Вы устроились на работу шахтера! Собирайте руду и зарабатывайте!");
                    SetPlayerSalary(player, 0);

                    CreateMineshaftObjects(player);
                    break;

                case (DialogID.MineshaftWorkEnd): // leave minechaft job
                    if (cancel)
                    {
                        return;
                    }

                    SetPlayerJob(player, null);
                    AddChatMessage(player, $"Вы уволились с работы шахтера. Вы заработали: {GetPlayerSalary(player)}");
                    SetPlayerMoney(player, GetPlayerMoney(player) + GetPlayerSalary(player));
                    SetPlayerSalary(player, 0);
                    ForceSavePlayerMoneyInDatabase(player);

                    DeleteMineshaftObjects(player);
                    break;

                case (DialogID.FrederikoHelpRequestDialog): // frede help dialog
                    if (cancel)
                    {
                        return;
                    }
                    ProcessHelpRequest(player, listItemId);
                    break;

                case (DialogID.CarRentChoose): // rent car dialog
                    if (cancel)
                    {
                        return;
                    }

                    switch (listItemId)
                    {
                        case (0):
                            if (GetPlayerMoney(player) < 20)
                            {
                                AddChatMessage(player, "Недостаточно средств для аренды.");
                                return;
                            }
                            SetPlayerMoney(player, GetPlayerMoney(player) - 20);

                            int newTransportId = new Random().Next(200000);
                            InstantiatePhotonBundleObject(player, newTransportId, "veh_RentCar", GetPlayerPosition(player));
                            AddChatMessage(player, "Вы успешно арендовали транспорт!");
                            SetPlayerRentedCarId(player, newTransportId);

                            ForceSavePlayerMoneyInDatabase(player);

                            break;
                    }

                    break;

                case (DialogID.CarRentStop): // delete rent car dialog
                    if (cancel)
                    {
                        return;
                    }

                    if (GetPlayerRentedCarId(player) != 0)
                    {
                        int oldTransportId = GetPlayerRentedCarId(player);
                        DestroyPhotonBundleObject(player, oldTransportId);
                        SetPlayerRentedCarId(player, null);
                    }

                    AddChatMessage(player, "Вы удалили арендованный транспорт.");
                    break;

                case (DialogID.BusWorkStart):
                    if (cancel)
                    {
                        return;
                    }

                    SetPlayerJob(player, "bus");
                    AddChatMessage(player, "Вы устроились на работу водителя автобуса! Выберите маршрут для поездки.");
                    SetPlayerSalary(player, 0);
                    CreateDialog(player, DialogID.BusWorkWaySelector);

                    break;

                case (DialogID.BusWorkEnd):
                    if (cancel)
                    {
                        return;
                    }

                    SetPlayerJob(player, null);
                    AddChatMessage(player, $"Вы уволились с работы водителя автобуса. Вы заработали: {GetPlayerSalary(player)}");
                    SetPlayerMoney(player, GetPlayerMoney(player) + GetPlayerSalary(player));
                    SetPlayerSalary(player, 0);
                    ForceSavePlayerMoneyInDatabase(player);

                    int busId = GetPlayerJobCarId(player);
                    DestroyPhotonBundleObject(player, busId);
                    SetPlayerJobCarId(player, null);

                    break;

                case (DialogID.BusWorkWaySelector):
                    if (cancel)
                    {
                        CreateDialog(player, DialogID.BusWorkWaySelector);
                        return;
                    }

                    StartBusJob(player, listItemId);

                    break;
            }
        }
    }
}
