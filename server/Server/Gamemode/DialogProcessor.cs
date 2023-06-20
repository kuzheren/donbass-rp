using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private void ProcessDialogResponse(Player player, int dialogId, int listItemId, bool cancel, string responseText, DialogID dialogEnum)
        {
            if (dialogId > 9 && dialogId < 17) // education
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
                            if (!IsPlayerEducated(player))
                            {
                                AddChatMessage(player, "Подойди к <color=green>Михаилу Фредерико</color>");
                                AddChatMessage(player, "Выбери пункт \"Квесты\", а после пункт \"Сын шахтёра\"");
                            }

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
                            CreateDialog(player, DialogID.AccountInfo, player.nickname, GetPlayerRegisterId(player), GetPlayerRegisterDateString(player), GetPlayerMoney(player), GetPlayerEXP(player));
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

                    JobID selectedJob = JobID.None;

                    switch (listItemId)
                    {
                        case (0):
                            selectedJob = JobID.Minechafter;

                            CreateMarker(player, new Vector3(-404.33f, 27.3f, 640.9116f));
                            break;

                        case (1):
                            selectedJob = JobID.BusDriver;

                            if (!CheckEXPPlayerJobAbility(player, selectedJob))
                            {
                                return;
                            }

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

                    SetPlayerJob(player, JobID.Minechafter);
                    AddChatMessage(player, "Вы устроились на работу шахтера! Собирайте руду и зарабатывайте!");
                    SetPlayerSalary(player, 0);

                    CreateMineshaftObjects(player);
                    break;

                case (DialogID.MineshaftWorkEnd): // leave minechaft job
                    if (cancel)
                    {
                        return;
                    }

                    SetPlayerJob(player, JobID.None);
                    AddChatMessage(player, $"Вы уволились с работы шахтера. Вы заработали: {GetPlayerSalary(player)}");
                    SetPlayerMoney(player, GetPlayerMoney(player) + GetPlayerSalary(player));
                    SetPlayerSalary(player, 0);
                    ForceSavePlayerMoneyInDatabase(player);

                    DeleteMineshaftObjects(player);
                    break;

                case (DialogID.FrederikoHelpRequestDialog): // frede help dialog
                    if (cancel)
                    {
                        CreateDialog(player, DialogID.FrederikoHelpChoose);
                        return;
                    }
                    ProcessHelpRequest(player, listItemId);
                    break;

                case (DialogID.FrederikoHelpChoose): // { "Квесты", "Помощь по серверу" }
                    {
                        if (cancel)
                        {
                            return;
                        }

                        switch (listItemId)
                        {
                            case (0):
                                {
                                    CreateDialog(player, DialogID.QuestMenu);
                                    break;
                                }

                            case (1):
                                {
                                    CreateDialog(player, DialogID.FrederikoHelpRequestDialog);
                                    break;
                                }
                        }

                        break;
                    }

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

                    SetPlayerJob(player, JobID.BusDriver);
                    AddChatMessage(player, "Вы устроились на работу водителя автобуса! Выберите маршрут для поездки.");
                    SetPlayerSalary(player, 0);
                    CreateDialog(player, DialogID.BusWorkWaySelector);

                    break;

                case (DialogID.BusWorkEnd):
                    if (cancel)
                    {
                        return;
                    }

                    SetPlayerJob(player, JobID.None);
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

                case (DialogID.AdminLogin):
                    {
                        if (cancel)
                        {
                            return;
                        }

                        if (responseText != Config.ADMIN_PASSWORD)
                        {
                            KickPlayer(player);
                            return;
                        }

                        SetPlayerAdminPermissions(player, true);
                        AddChatMessage(player, "Вы успешно вошли как админ!");

                        break;
                    }

                case (DialogID.AdminPlayerMenu):
                    {
                        if (cancel)
                        {
                            return;
                        }

                        if (!IsAdmin(player))
                        {
                            return;
                        }

                        Player adminTarget = GetAdminActionsTargetPlayer(player);

                        switch (listItemId) // new string[] { "Телепортироваться к игроку", "Телепортировать к себе", "Отправить игроку сообщение", "Кикнуть игрока", "Забанить игрока", "Выдать игроку деньги", "Выдать игроку достижение" } } },
                        {
                            case (0):
                                TeleportPlayer(player, GetPlayerPosition(adminTarget));
                                break;

                            case (1):
                                TeleportPlayer(adminTarget, GetPlayerPosition(player));
                                break;

                            case (2):
                                CreateDialog(player, DialogID.AdminMessageToPlayer);
                                break;

                            case (3):
                                KickPlayer(adminTarget);
                                break;

                            case (4):
                                break;

                            case (5):
                                CreateDialog(player, DialogID.AdminGiveMoney);
                                break;

                            case (6):
                                CreateDialog(player, DialogID.AdminGiveAchievement);
                                break;
                        }

                        break;
                    }

                case (DialogID.AdminMessageToPlayer):
                    {
                        if (cancel)
                        {
                            CreateDialog(player, DialogID.AdminPlayerMenu);
                            return;
                        }

                        Player adminTarget = GetAdminActionsTargetPlayer(player);

                        AddChatMessage(adminTarget, responseText);

                        CreateDialog(player, DialogID.AdminPlayerMenu);

                        break;
                    }

                case (DialogID.AdminGiveMoney):
                    {
                        if (cancel)
                        {
                            CreateDialog(player, DialogID.AdminPlayerMenu);
                            return;
                        }

                        Player adminTarget = GetAdminActionsTargetPlayer(player);
                        int money = int.Parse(responseText);

                        SetPlayerMoney(adminTarget, GetPlayerMoney(adminTarget) + money);
                        ForceSavePlayerMoneyInDatabase(adminTarget);

                        break;
                    }

                case (DialogID.AdminGiveAchievement):
                    {
                        if (cancel)
                        {
                            CreateDialog(player, DialogID.AdminPlayerMenu);
                            return;
                        }

                        Player adminTarget = GetAdminActionsTargetPlayer(player);
                        int achievementID = int.Parse(responseText);

                        GiveAchievement(adminTarget, achievementID);

                        break;
                    }

                case (DialogID.AdminPanel): // "Включить музыку у игроков", "Начать бомбардировку", "Отправить глобальное сообщение", "Кикнуть всех (кроме себя)", "Закрыть/Открыть сервер"
                    {
                        if (cancel)
                        {
                            return;
                        }

                        switch (listItemId)
                        {
                            case (0):
                                CreateDialog(player, DialogID.AdminMusicChange);
                                break;

                            case (1):
                                AddChatMessage(player, "Вы начали бомбардировку!");
                                StartBombingEvent();
                                break;

                            case (2):
                                AddGlobalChatMessage(responseText);
                                break;

                            case (3):
                                foreach (Player serverPlayer in PlayersList)
                                {
                                    if (serverPlayer != player)
                                    {
                                        KickPlayer(serverPlayer);
                                    }
                                }
                                break;

                            case (4):
                                SetServerOpenState(!GetServerOpenState());
                                AddChatMessage(player, $"Вы {(GetServerOpenState() ? "открыли" : "закрыли")} сервер для входа новых игроков");
                                break;
                        }

                        break;
                    }

                case (DialogID.AdminMusicChange):
                    {
                        if (cancel)
                        {
                            return;
                        }

                        PlayGlobalAudiostream(responseText, isLoop: true);

                        break;
                    }


                case (DialogID.QuestMenu):
                    {
                        if (cancel)
                        {
                            CreateDialog(player, DialogID.FrederikoHelpChoose);
                            return;
                        }

                        CreateQuestDialog(player, listItemId);

                        break;
                    }

                case (DialogID.QuestInfo):
                    {
                        CreateDialog(player, DialogID.QuestMenu);
                        break;
                    }

                case (DialogID.PromoCodeDialog):
                    {
                        ProcessPromoCode(player, responseText);

                        CreateDialog(player, DialogID.EducationChooseDialog);

                        break;
                    }

                case (DialogID.EducationChooseDialog):
                    {
                        if (cancel)
                        {
                            CreateDialog(player, DialogID.EducationChooseDialog);
                            return;
                        }

                        if (listItemId == 0)
                        {
                            CreateDialog(player, DialogID.EducationStart);
                        }
                        else if (listItemId == 1)
                        {
                            CreateDialog(player, DialogID.SpawnChoose);
                        }

                        break;
                    }
            }
        }
    }
}
