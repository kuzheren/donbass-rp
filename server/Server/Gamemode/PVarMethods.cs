using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using static ProtonServer.Gamemode;


namespace ProtonServer
{
    public partial class Gamemode
    {
        private int GetPlayerMoney(Player player)
        {
            return (int)GetPVar(player, PVar.Money);
        }
        private void SetPlayerMoney(Player player, int money)
        {
            SetPVar(player, PVar.Money, money);

            UpdateMoneyPanel(player);
        }

        private float GetPlayerHealth(Player player)
        {
            if (GetPVar(player, PVar.Health) == null)
            {
                return 0f;
            }
            return (float)GetPVar(player, PVar.Health);
        }
        private void SetPlayerHealth(Player player, float health, bool sendToClient=true)
        {
            SetPVar(player, PVar.Health, health);
            SendSetHealth(player, health);
        }

        private string GetPlayerToken(Player player)
        {
            return (string)GetPVar(player, PVar.Token);
        }
        private void SetPlayerToken(Player player, string token)
        {
            SetPVar(player, PVar.Token, token);
        }

        private bool IsAndroidPlayer(Player player)
        {
            return (bool)GetPVar(player, PVar.IsAndroid);
        }
        private void SetAndroidPlayerState(Player player, bool state)
        {
            SetPVar(player, PVar.IsAndroid, state);
        }

        private Vector3 GetPlayerPosition(Player player)
        {
            return (Vector3)GetPVar(player, PVar.Position);
        }
        private void StorePlayerPosition(Player player, Vector3 position)
        {
            SetPVar(player, PVar.Position, position);
        }

        private WeaponEnum GetCurrentPlayerWeapon(Player player)
        {
            return (WeaponEnum)GetPVar(player, PVar.CurrentWeaponType);
        }
        private void StoreCurrentPlayerWeapon(Player player, WeaponEnum weaponType)
        {
            SetPVar(player, PVar.CurrentWeaponType, weaponType);
        }

        private bool IsPlayerLogged(Player player)
        {
            return (string)GetPVar(player, PVar.Token) != "";
        }

        private int GetPlayerSalary(Player player)
        {
            if (GetPVar(player, PVar.Salary) == null)
            {
                return 0;
            }
            else
            {
                return (int)GetPVar(player, PVar.Salary);
            }
        }
        private void SetPlayerSalary(Player player, int salary)
        {
            if (salary == 0)
            {
                SetPVar(player, PVar.Salary, null);
            }
            else
            {
                SetPVar(player, PVar.Salary, salary);
            }
        }

        private List<int> GetPlayerMineshaftOres(Player player)
        {
            return (List<int>)GetPVar(player, PVar.MineshaftOreIds);
        }
        private void SetPlayerMineshaftOres(Player player, List<int> ores)
        {
            SetPVar(player, PVar.MineshaftOreIds, ores);
        }

        private int GetPlayerAmmo(Player player, WeaponEnum weaponType)
        {
            if (GetPVar(player, weaponType.ToString()) == null)
            {
                return 0;
            }
            return (int)GetPVar(player, weaponType.ToString());
        }
        private void StorePlayerAmmo(Player player, WeaponEnum weaponType, int ammount)
        {
            SetPVar(player, weaponType.ToString(), ammount);
        }

        private JobID GetPlayerJob(Player player)
        {
            int jobCode = (int)GetPVar(player, PVar.CurrentJob);
            return (JobID)Enum.ToObject(typeof(JobID), jobCode);
        }
        private void SetPlayerJob(Player player, JobID jobId)
        {
            SetPVar(player, PVar.CurrentJob, (int)jobId);
        }

        private int GetPlayerRentedCarId(Player player)
        {
            object rentedCarId = GetPVar(player, PVar.RentedCarId);
            if (rentedCarId == null)
            {
                return 0;
            }

            return (int)rentedCarId;
        }
        private void SetPlayerRentedCarId(Player player, object rentedCarId)
        {
            SetPVar(player, PVar.RentedCarId, rentedCarId);
        }

        private int GetPlayerJobCarId(Player player)
        {
            object jobCarId = GetPVar(player, PVar.JobCarId);
            if (jobCarId == null)
            {
                return 0;
            }

            return (int)jobCarId;
        }
        private void SetPlayerJobCarId(Player player, object rentedCarId)
        {
            SetPVar(player, PVar.JobCarId, rentedCarId);
        }

        private void LoadPlayerAdminPermissions(Player player)
        {
            bool isAdmin = GetDBVar(player, DBVar.IsAdmin) == "true";
            SetPVar(player, PVar.IsAdmin, isAdmin);
        }
        private bool IsAdmin(Player player)
        {
            return (bool)GetPVar(player, PVar.IsAdmin);
        }

        private void SetAdminActionsTargetPlayer(Player admin, Player target)
        {
            SetPVar(admin, PVar.AdminTarget, target);
        }
        private Player GetAdminActionsTargetPlayer(Player admin)
        {
            return (Player)GetPVar(admin, PVar.AdminTarget);
        }

        private bool IsQuestPassed(Player player, QuestID questId)
        {
            object questObject = GetPVar(player, $"QUEST_{questId}");
            if (questObject == null)
            {
                return false;
            }

            return (bool)GetPVar(player, $"QUEST_{questId}");
        }
        private void SetQuestPassed(Player player, QuestID questId)
        {
            SetPVar(player, $"QUEST_{questId}", true);
        }

        private int GetPlayerEXP(Player player)
        {
            return (int)GetPVar(player, PVar.EXP);
        }
        private void SetPlayerEXP(Player player, int EXP)
        {
            SetPVar(player, PVar.EXP, EXP);
        }

        private void SetQuestProgress(Player player, QuestID questId, int progress, int maxProgress)
        {
            string questName = questInfo.ElementAt((int)questId).Key;

            SetPVar(player, $"QUESTPROGRESS{questId}", progress);

            if (progress > 0 && progress < maxProgress)
            {
                AddChatMessage(player, $"Прогресс квеста ({questName}): {progress}/{maxProgress}");
            }
            else if (progress >= maxProgress)
            {
                AddChatMessage(player, $"Поздравляем! Вы выполнили квест \"{questName}\".");
                AddChatMessage(player, "Награда была выдана автоматически.");
                ProcessQuestCompletedEvent(player, questId);
            }
        }
        private int GetQuestProgress(Player player, QuestID questId)
        {
            object questProgressObject = GetPVar(player, $"QUESTPROGRESS{questId}");
            if (questProgressObject == null)
            {
                return 0;
            }

            return (int)questProgressObject;
        }

        private void SetPlayerRegisterDate(Player player, int date)
        {
            SetPVar(player, PVar.RegisterDate, date);
        }
        private int GetPlayerRegisterDate(Player player)
        {
            object objectRegisterDate = GetPVar(player, PVar.RegisterDate);
            if (objectRegisterDate == null)
            {
                return 0;
            }

            return (int)objectRegisterDate;
        }
        private string GetPlayerRegisterDateString(Player player)
        {
            int registerTime = GetPlayerRegisterDate(player);

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(registerTime);
            string formattedString = dateTime.ToString("yyyy.MM.dd HH:mm:ss");

            return formattedString;
        }

        private void SetPlayerRegisterId(Player player, int Id)
        {
            SetPVar(player, PVar.RegisterId, Id);
        }
        private int GetPlayerRegisterId(Player player)
        {
            object objectRegisterId = GetPVar(player, PVar.RegisterId);
            if (objectRegisterId == null)
            {
                return 0;
            }

            return (int)objectRegisterId;
        }

        private void SetBombingLivePlayerState(Player player, bool state)
        {
            SetPVar(player, PVar.LiveOnBombing, state);
        }
        private bool GetBombingLivePlayerState(Player player)
        {
            object objectBombingLive = GetPVar(player, PVar.LiveOnBombing);
            if (objectBombingLive == null)
            {
                return false;
            }

            return (bool)objectBombingLive;
        }
    }
}
