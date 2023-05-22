using LiteNetLib;
using System.Net;
using System.Net.Sockets;


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

        private string GetPlayerJob(Player player)
        {
            return (string)GetPVar(player, PVar.CurrentJob);
        }
        private void SetPlayerJob(Player player, string jobName)
        {
            SetPVar(player, PVar.CurrentJob, jobName);
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
    }
}
