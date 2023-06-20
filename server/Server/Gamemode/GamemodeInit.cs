using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        public Server server;
        public ServerLogic serverLogic;
        public List<Player> PlayersList => serverLogic.players;
        public Dictionary<string, object> ServerProperties = new Dictionary<string, object>();
        private RpcListener rpcListener => server.rpcListener;

        private enum DialogType
        {
            CHOOSE = 1, // диалоговое меню с многими кнопками (меню сервера)
            INPUT = 2, // диалоговое меню с полем ввода (пароль...)
            INFO = 3 // диалоговое окно с текстом (инфа о сервере)
        }
        private enum DamageTargetType
        {
            AIR = 0,
            OBSTACLE = 1,
            PLAYER = 2,
            VEHICLE = 3
        }
        private enum DBVar
        {
            Money,
            IsEducated,
            LastPosition,
            IsAdmin,
            QuestsInfo,
            EXP
        }
        private enum PVar
        {
            Token,
            Health,
            Money,
            IsAndroid,
            Salary,
            Position,
            CurrentWeaponType,
            RentedCarId,
            JobCarId,
            CurrentJob,
            EXP,

            MineshaftOreIds,

            IsAdmin,
            AdminTarget,

            RegisterDate,
            RegisterId,

            LiveOnBombing,
        }
        private enum SVar
        {
            IsBombing,
            IsServerOpened,
        }

        private struct ShootHole
        {
            public Vector3 position;
            public Vector3 normal;
        }
        private struct MappingObject
        {
            public int id;
            public string name;
            public Vector3 position;
            public Vector3 rotation;
        }
        private struct BundleTransport
        {
            public int id;
            public string name;
            public Vector3 position;
            public Vector3 rotation;
        }

        private List<int> openedDoors = new List<int>();
        private List<ShootHole> shootHoles = new List<ShootHole>();
        private List<MappingObject> mappingObjects = new List<MappingObject>();

        public Gamemode(Server server, ServerLogic serverLogic)
        {
            this.server = server;
            this.serverLogic = serverLogic;

            new Thread(() =>
            {
                while (true)
                {
                    Config.Reload();
                    Thread.Sleep(1000);
                }
            }).Start();
        }

        private void InitRPCCallbacks()
        {
            rpcListener.AddCallback("Rpc_DialogResponse", Rpc_DialogResponse);
            rpcListener.AddCallback("Rpc_UpdatePosition", Rpc_UpdatePosition);
            rpcListener.AddCallback("Rpc_GetPickup", Rpc_GetPickup);
            rpcListener.AddCallback("Rpc_OpenDoor", Rpc_OpenDoor);
            rpcListener.AddCallback("Rpc_ClickTextdraw", Rpc_ClickTextdraw);
            rpcListener.AddCallback("Rpc_Shoot", Rpc_Shoot);
            rpcListener.AddCallback("Rpc_RocketExplosion", Rpc_RocketExplosion);
            rpcListener.AddCallback("Rpc_UpdateHealth", Rpc_UpdateHealth);
            rpcListener.AddCallback("Rpc_ChangeWeapon", Rpc_ChangeWeapon);
            rpcListener.AddCallback("Rpc_ClickEntity", Rpc_ClickEntity);
            rpcListener.AddCallback("Rpc_ClickPlayer", Rpc_ClickPlayer);
            rpcListener.AddCallback("Rpc_AdUnavailable", Rpc_AdUnavailable);
            rpcListener.AddCallback("Rpc_SendAdShowed", Rpc_SendAdShowed);
            rpcListener.AddCallback("Rpc_EnterTransport", Rpc_EnterTransport);
            rpcListener.AddCallback("Rpc_QuitTransport", Rpc_QuitTransport);
        }
    }
}
