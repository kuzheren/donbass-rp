using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        public void OnChatCommand(Player player, string cmd, string argumentsString, string[] argumentsList)
        {
            ProcessChatCommand(player, cmd, argumentsString, argumentsList);
        }

        private void ProcessChatCommand(Player player, string cmd, string argumentsString, string[] argumentsList)
        {
            switch (cmd)
            {
                case ("help"):
                    {
                        CreateDialog(player, DialogID.Help);
                        break;
                    }

                case ("jobs"):
                    {
                        if (!IsPlayerLogged(player))
                        {
                            AddChatMessage(player, "Войдите в аккаунт, чтобы начать работу.");
                            return;
                        }
                        CreateDialog(player, DialogID.JobSelector);
                        break;
                    }

                case ("report"):
                    {
                        CreateDialog(player, DialogID.Report);
                        break;
                    }

                case ("pay"):
                    {
                        if (argumentsList.Length != 2)
                        {
                            AddChatMessage(player, "Введите аргументы: <ID игрока> <сумма>");
                            return;
                        }

                        int playerId = int.Parse(argumentsList[0]);
                        int money = int.Parse(argumentsList[1]);
                        Player target = GetPlayerById(playerId);

                        if (money <= 0)
                        {
                            AddChatMessage(player, $"Вы не можете взять деньги в долг.");
                            return;
                        }

                        if (target == null)
                        {
                            AddChatMessage(player, $"Нет игрока с ID {playerId}");
                            return;
                        }

                        if (GetPlayerMoney(player) < money)
                        {
                            AddChatMessage(player, $"Недостаточно средств.");
                            return;
                        }

                        if (player == target)
                        {
                            AddGlobalChatMessage($"Игрок {player} переложил из одного кармана в другой {money} денег.");
                            return;
                        }

                        SetPlayerMoney(player, GetPlayerMoney(player) - money);
                        SetPlayerMoney(target, GetPlayerMoney(target) + money);
                        AddGlobalChatMessage($"{player.nickname} передал {money} денег игроку {target.nickname}");

                        ForceSavePlayerMoneyInDatabase(player);
                        ForceSavePlayerMoneyInDatabase(target);

                        break;
                    }

                case ("alogin"):
                    {
                        if (!IsPlayerLogged(player))
                        {
                            return;
                        }

                        if (IsAdmin(player))
                        {
                            AddChatMessage(player, "Вы уже администратор!");
                            return;
                        }

                        CreateDialog(player, DialogID.AdminLogin);
                        break;
                    }

                case ("apanel"):
                    {
                        if (!IsPlayerLogged(player))
                        {
                            return;
                        }

                        if (!IsAdmin(player))
                        {
                            return;
                        }

                        CreateDialog(player, DialogID.AdminPanel);
                        break;
                    }

                case ("raise"):
                    {
                        Exception exception = new InvalidOperationException("Пошел нахуй");

                        CreateDialog(player, DialogID.ServerError, exception.GetType().Name, exception.Message);
                        CreateErrorTraceTextdraw(player, exception);

                        break;
                    }

                case ("query"):
                    {
                        if (!IsAdmin(player))
                        {
                            return;
                        }

                        Dictionary<string, string> result = ExecuteQuery(argumentsString);
                        AddChatMessage(player, "QUERY REESULT:");
                        foreach (string key in result.Keys)
                        {
                            AddChatMessage(player, $"{key}: {result[key]}");
                        }

                        break;
                    }

                case ("bunker"):
                    {
                        CreateClosestBunkerPickup(player);
                        break;
                    }

                default:
                    {
                        AddChatMessage(player, $"Не удалось найти команду \"{cmd}\". Введите /help для справки");
                        break;
                    }
            }
        }
    }
}
