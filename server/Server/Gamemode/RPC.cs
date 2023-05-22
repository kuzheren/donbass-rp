using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        public void Rpc_DialogResponse(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int dialogId = (int)arguments[0].value;
            int listItemId = (int)arguments[1].value;
            bool cancel = (bool)arguments[2].value;
            string responseText = (string)arguments[3].value;

            DialogID dialogEnum = default;
            try
            {
                dialogEnum = (DialogID)Enum.ToObject(typeof(DialogID), dialogId);
            }
            catch
            {
            }

            ProcessDialogResponse(player, dialogId, listItemId, cancel, responseText, dialogEnum);
        }

        public void Rpc_UpdatePosition(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            Vector3 position = (Vector3)arguments[0].value;

            StorePlayerPosition(player, position);

            if (position.y < -30)
            {
                ProcessPlayerDeathEvent(player);
            }
        }

        public void Rpc_GetPickup(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int pickupId = (int)arguments[0].value;

            ProcessPlayerGetPickup(player, pickupId);
        }

        public void Rpc_OpenDoor(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int doorId = (int)arguments[0].value;
            bool open = (bool)arguments[1].value;

            ProcessOpenDoorEvent(player, doorId, open);
        }

        public void Rpc_ClickTextdraw(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int textdrawId = (int)arguments[0].value;

            ProcessClickTextdraw(player, textdrawId);
        }

        public void Rpc_Shoot(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            DamageTargetType damageTarget = (DamageTargetType)Enum.ToObject(typeof(DamageTargetType), (int)arguments[0].value);
            int bulletTargetId = (int)arguments[1].value;
            float damage = (float)arguments[2].value;
            Vector3 destination = (Vector3)arguments[3].value;
            float distance = (float)arguments[4].value;
            Vector3 hitNormal = (Vector3)arguments[5].value;
            arguments.Add(new NetworkValue(player.Id));

            ProcessPlayerShoot(player, damageTarget, bulletTargetId, damage, destination, distance, hitNormal, arguments);
        }

        public void Rpc_ChangeWeapon(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int weaponId = (int)arguments[0].value;
            WeaponEnum weaponType = (WeaponEnum)Enum.ToObject(typeof(WeaponEnum), weaponId);

            SendGlobalRPCExcept(player, "Rpc_ChangeWeapon", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(player.Id), new NetworkValue(weaponId) });

            StoreCurrentPlayerWeapon(player, weaponType);
        }

        public void Rpc_RocketExplosion(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            Vector3 position = (Vector3)arguments[0].value;
        }

        public void Rpc_UpdateHealth(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            float newHealth = (float)arguments[0].value;

            if (newHealth > 0f)
            {
                SetPlayerHealth(player, newHealth, false);
            }
            else
            {
                ProcessPlayerDeathEvent(player);
            }
        }

        public void Rpc_ClickEntity(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int entityId = (int)arguments[0].value;

            ProcessClickEntityEvent(player, entityId);
            //DestroyGlobalObject(entityId);
        }

        public void Rpc_ClickPlayer(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            string playerNickname = (string)arguments[0].value;
            Player targetPlayer = GetPlayerByNickname(playerNickname);

            if (targetPlayer == null)
            {
                return;
            }

            ProcessClickPlayerEvent(player, targetPlayer);
        }

        public void Rpc_AdUnavailable(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            bool isVideo = (bool)arguments[0].value;
        }

        public void Rpc_SendAdShowed(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            bool isVideo = (bool)arguments[0].value;
        }

        public void Rpc_EnterTransport(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int Id = (int)arguments[0].value;

            ProcessPlayerEnterTransport(player, Id);
        }

        public void Rpc_QuitTransport(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int Id = (int)arguments[0].value;

            ProcessPlayerQuitTransport(player, Id);
        }
    }
}
