using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private void InitDoors(Player player)
        {
            List<NetworkValue> networkDoors = ProtonTypes.ConvertToNetworkValuesList(openedDoors);

            SendRPC(player, "Rpc_InitDoors", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(networkDoors) });
        }
        private void InitBulletHoles(Player player)
        {
            ProtonStream protonStream = new ProtonStream();

            protonStream.Write(Config.MAX_BULLET_HOLES);
            protonStream.Write(shootHoles.Count);

            foreach (ShootHole shootHole in shootHoles)
            {
                protonStream.Write(shootHole.position);
                protonStream.Write(shootHole.normal);
            }

            SendRPC(player, "Rpc_InitBulletHoles", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(protonStream.Bytes.ToArray()) });
        }
        private void InitMapping(Player player)
        {
            foreach (MappingObject mappingObject in mappingObjects)
            {
                CreateObject(player, mappingObject.name, mappingObject.position, mappingObject.rotation, mappingObject.id);
            }
        }
        private void InitTextdraws(Player player)
        {
            InitTextdrawImage(player, "gz");
            InitTextdrawImage(player, "ptr");
            InitTextdrawImage(player, "money");
            InitTextdrawImage(player, "pos");
            InitTextdrawImage(player, "exception");

            UpdateMoneyPanel(player);
            CreateTextdraw(player, 2, "gz", true, new Vector2(-542.1f + 601, 290.5f), new Vector2(198.65f, 185.78f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), onMap: true);
            CreateTextdraw(player, 3, "gz", true, new Vector2(-848.8f + 601, -129.6f), new Vector2(172.9f, 185.7f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), onMap: true);
            CreateTextdraw(player, 4, "gz", true, new Vector2(-976f + 601, -373f), new Vector2(140.7f, 110.7f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), onMap: true);
            CreateTextdraw(player, 5, "gz", true, new Vector2(-371f + 601, -332.09f), new Vector2(200.79f, 149.3f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), onMap: true);
        }
        private void InitPickups(Player player)
        {
            foreach (int id in staticPickupInfo.Keys)
            {
                Vector3 position = staticPickupInfo[id];

                CreatePickup(player, position, id);
            }
        }

        private void CreateInitGameData(ref Dictionary<string, NetworkValue> customData)
        {
            customData["syncRate"] = new NetworkValue(SYNC_RATE);
            customData["weaponData"] = new NetworkValue(CreateWeaponsDataStructure());
        }
    }
}
