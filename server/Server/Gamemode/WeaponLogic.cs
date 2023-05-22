using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        public enum WeaponEnum
        {
            None = 0,
            UZY = 1,
            AK78 = 2,
            Ashotgun = 3,
            AVP = 4,
            MisterRocketLauncher = 5
        }
        public class WeaponInfo
        {
            public WeaponEnum weaponType = WeaponEnum.None;
            public float fireRate = 1;
            public float damage = 0;
            public float spread = 0;
            public float maxDistance = 0;
            public float reloadTime = 1;
            public int magazineSize = 0;
        }

        private byte[] GetSerializedWeaponData(WeaponInfo weaponInfo)
        {
            ProtonStream PS = new ProtonStream();
            PS.Write((int)weaponInfo.weaponType);
            PS.Write(weaponInfo.fireRate);
            PS.Write(weaponInfo.damage);
            PS.Write(weaponInfo.spread);
            PS.Write(weaponInfo.maxDistance);
            PS.Write(weaponInfo.reloadTime);
            PS.Write(weaponInfo.magazineSize);

            return PS.Bytes.ToArray();
        }
        private Dictionary<string, NetworkValue> CreateWeaponsDataStructure()
        {
            Dictionary<string, NetworkValue> dictionary = new Dictionary<string, NetworkValue>();

            WeaponInfo UZYData = new WeaponInfo()
            {
                weaponType = WeaponEnum.UZY,
                fireRate = 10,
                damage = 2.5f,
                spread = 2,
                maxDistance = 100,
                reloadTime = 2,
                magazineSize = 30
            };

            WeaponInfo AK47Data = new WeaponInfo()
            {
                weaponType = WeaponEnum.AK78,
                fireRate = 8,
                damage = 4.4f,
                spread = 3,
                maxDistance = 150,
                reloadTime = 2,
                magazineSize = 20
            };

            WeaponInfo ShotgunData = new WeaponInfo()
            {
                weaponType = WeaponEnum.Ashotgun,
                fireRate = 0.5f,
                damage = 15,
                spread = 10,
                maxDistance = 50,
                reloadTime = 5,
                magazineSize = 6
            };

            WeaponInfo AWPData = new WeaponInfo()
            {
                weaponType = WeaponEnum.AVP,
                fireRate = 1,
                damage = 150,
                spread = 0,
                maxDistance = 10000,
                reloadTime = 7,
                magazineSize = 6
            };

            WeaponInfo RPGData = new WeaponInfo()
            {
                weaponType = WeaponEnum.MisterRocketLauncher,
                fireRate = 0.2f,
                //fireRate = 20f,
                damage = 500,
                spread = 0,
                maxDistance = 1000,
                reloadTime = 5,
                //reloadTime = 1,
                magazineSize = 1
            };

            dictionary["UZY"] = new NetworkValue(GetSerializedWeaponData(UZYData));
            dictionary["AK78"] = new NetworkValue(GetSerializedWeaponData(AK47Data));
            dictionary["Ashotgun"] = new NetworkValue(GetSerializedWeaponData(ShotgunData));
            dictionary["AVP"] = new NetworkValue(GetSerializedWeaponData(AWPData));
            dictionary["RPG"] = new NetworkValue(GetSerializedWeaponData(RPGData));

            return dictionary;
        }

        private bool ValidateShoot(Player player, DamageTargetType damageTarget, int targetId, float damage, Vector3 destination, float distance, Vector3 hitNormal)
        {
            if (!ValidateGreenZoneShoot(player, damageTarget, targetId, damage, destination, distance))
            {
                CreateDialog(player, DialogID.ShootingForbidden);
                return false;
            }
            return true;
        }
        private bool ValidateGreenZoneShoot(Player player, DamageTargetType damageTarget, int targetId, float damage, Vector3 destination, float distance)
        {
            foreach ((Vector3 min, Vector3 max) zone in greenZones)
            {
                if (destination.x >= zone.min.x && destination.x <= zone.max.x && destination.z >= zone.min.z && destination.z <= zone.max.z)
                {
                    if (damageTarget == DamageTargetType.PLAYER)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private void ProcessPlayerShoot(Player player, DamageTargetType damageTarget, int bulletTargetId, float damage, Vector3 destination, float distance, Vector3 hitNormal, List<NetworkValue> arguments)
        {
            if (!ValidateShoot(player, damageTarget, bulletTargetId, damage, destination, distance, hitNormal))
            {
                return;
            }

            if (damageTarget == DamageTargetType.OBSTACLE)
            {
                ShootHole shootHole = new ShootHole() { position = destination, normal = hitNormal };
                shootHoles.Add(shootHole);

                if (shootHoles.Count > Config.MAX_BULLET_HOLES)
                {
                    shootHoles.RemoveAt(0);
                }
            }

            SendGlobalRPCExcept(player, "Rpc_Shoot", DeliveryMethod.ReliableOrdered, arguments);

            if (damageTarget == DamageTargetType.PLAYER && bulletTargetId != 0)
            {
                Player targetPlayer = GetPlayerById(bulletTargetId);
                if (targetPlayer != null)
                {
                    float newHealth = GetPlayerHealth(player) - damage;
                    if (newHealth > 0f)
                    {
                        SetPlayerHealth(player, newHealth);
                    }
                    else
                    {
                        ProcessPlayerDeathEvent(targetPlayer);
                    }
                }
            }

            WeaponEnum currentWeaponType = GetCurrentPlayerWeapon(player);
            StorePlayerAmmo(player, currentWeaponType, GetPlayerAmmo(player, currentWeaponType) - 1);
        }
    }
}
