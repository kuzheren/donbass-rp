using LiteNetLib;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;

namespace ProtonServer
{
    public class Gamemode
    {
        #region Initialization
        private Server server;
        private ServerLogic serverLogic;
        private List<Player> PlayersList => serverLogic.players;
        private RpcListener rpcListener => server.rpcListener;

        private int delay = 10000;

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
        private enum WeaponEnum
        {
            None = 0,
            UZY = 1,
            AK78 = 2,
            Ashotgun = 3,
            AVP = 4,
            MisterRocketLauncher = 5
        }
        private struct ShootHole
        {
            public Vector3 position;
            public Vector3 normal;
        }

        private List<int> openedDoors = new List<int>();
        private List<ShootHole> shootHoles = new List<ShootHole>();

        public Gamemode(Server server, ServerLogic serverLogic)
        {
            this.server = server;
            this.serverLogic = serverLogic;
        }
        #endregion

        #region Callbacks
        public void OnServerStarted()
        {
            Utils.Log("Gamemode started!");

            InitRPCCallbacks();
            new Thread(SpawnBombingPlanes).Start();
        }

        public void OnPeerConnected(Player player)
        {
        }

        public void OnPlayerDisconnected(Player player, DisconnectInfo disconnectInfo)
        {
            AddGlobalChatMessage($"Игрок {player.nickname} вышел из игры по причине: {disconnectInfo.Reason}. Ну и пошел нахуй!");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnChatCommand(Player player, string cmd, string argumentsString, string[] argumentsList)
        {
            switch (cmd)
            {
                case ("help"):
                    CreateDialog(player, 1);
                    break;

                case ("tp"):
                    TeleportAllPlayers((Vector3)GetPlayerProperty(player, "position"));
                    break;

                case ("car"):
                    CreateVehicle(player, "Vehicle", (Vector3)GetPlayerProperty(player, "position"), new Vector3());
                    break;

                case ("plane"):
                    TeleportPlayer(player, new Vector3(-709.7776f, -9.6f, -634.0363f));
                    CreateAirplane(player, "AirPlane", new Vector3(new Random().Next(-733, -693), -7f, -616f), new Vector3());
                    break;

                case ("spawn"):
                    RespawnPlayer(player);
                    break;

                case ("kick"):
                    if (argumentsList.Length != 1)
                    {
                        return;
                    }

                    Player kickedPlayer = GetPlayerByNickname(argumentsList[0]);
                    if (kickedPlayer != null)
                    {
                        KickPlayer(kickedPlayer);
                    }

                    break;

                case ("prate"):
                    if (argumentsList.Length != 1)
                    {
                        return;
                    }

                    delay = int.Parse(argumentsList[0]);
                    AddGlobalChatMessage($"Задержка спавна самолётов установлена на {delay}. Теперь в секунду спавнится {1000f / delay} самолёт(ов).");
                    break;

                case ("object"):
                    Vector3 cockPosition = (Vector3)GetPlayerProperty(player, "position") + new Vector3(0, 3, 0);
                    CreateGlobalObject("map_Cock", cockPosition, objectId: 333);
                    AddGlobalChatMessage($"Мы заспавнили объект маппинга: Хуй. Коорды: {cockPosition}");
                    break;

                case ("music"):
                    CreateDialog(player, 6);
                    break;

                default:
                    AddChatMessage(player, $"Команда \"{cmd}\" не найдена. Введите /help для помощи.");
                    break;
            }
        }

        public void OnChatMessage(Player player, string message)
        {
            AddGlobalChatMessage($"{player.nickname}: {message}");
        }
        #endregion

        #region Hooked Callbacks
        public bool OnConnectionRequest(ConnectionRequest request, string key)
        {
            if (key != Config.SERVER_KEY)
            {
                // implement outdated version message
            }
            return key == Config.SERVER_KEY;
        }

        public bool OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            return true;
        }

        public bool OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            return true;
        }

        public bool OnInitConnection(Player player, ConnectionRequestInfo connectionRequestInfo)
        {
            if (ValidateConnectionRequestAndKickIfNot(player, connectionRequestInfo))
            {
                ProcessPlayerJoinedEvent(player);
            }

            return true;
        }

        public bool OnReceivePacket(PacketId packetId, ProtonStream data, Dictionary<string, NetworkValue> customPacketData, DeliveryMethod deliveryMethod)
        {
            return true;
        }

        public bool OnReceiveRPC(int rpcId, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            return true;
        }
        #endregion

        #region Methods
        private object GetPlayerProperty(Player player, string propertyName)
        {
            if (player.properties.ContainsKey(propertyName))
            {
                return player.properties[propertyName];
            }
            return null;
        }

        private void SetPlayerProperty(Player player, string propertyName, object property)
        {
            player.properties[propertyName] = property;
        }

        private void KickPlayer(Player player)
        {
            server.KickPlayer(player);
        }

        private void AddChatMessage(Player player, string message)
        {
            player.SendChatMessage(message);
        }

        private void AddGlobalChatMessage(string message)
        {
            foreach (Player player in PlayersList)
            {
                AddChatMessage(player, message);
            }
        }

        private Player GetPlayerById(int Id)
        {
            foreach (Player player in PlayersList)
            {
                if (player.Id == Id)
                {
                    return player;
                }
            }
            return null;
        }

        private Player GetPlayerByNickname(string nickname)
        {
            foreach (Player player in PlayersList)
            {
                if (player.nickname == nickname)
                {
                    return player;
                }
            }
            return null;
        }

        private Player GetPlayerByIP(IPEndPoint endPoint)
        {
            foreach (Player player in PlayersList)
            {
                if (player.playerPeer.EndPoint == endPoint)
                {
                    return player;
                }
            }
            return null;
        }

        private void SendRPC(Player player, int rpcId, DeliveryMethod deliveryMethod, List<NetworkValue> arguments)
        {
            player.SendRPC(rpcId, deliveryMethod, arguments);
        }

        private void SendRPC(Player player, string rpcId, DeliveryMethod deliveryMethod, List<NetworkValue> arguments)
        {
            SendRPC(player, RpcListener.GetStringHash(rpcId), deliveryMethod, arguments);
        }

        private void SendGlobalRPC(int rpcId, DeliveryMethod deliveryMethod, List<NetworkValue> arguments)
        {
            foreach (Player player in PlayersList)
            {
                SendRPC(player, rpcId, deliveryMethod, arguments);
            }
        }

        private void SendGlobalRPC(string rpcId, DeliveryMethod deliveryMethod, List<NetworkValue> arguments)
        {
            SendGlobalRPC(RpcListener.GetStringHash(rpcId), deliveryMethod, arguments);
        }

        private void SendGlobalRPCExcept(Player exceptedPlayer, string rpcId, DeliveryMethod deliveryMethod, List<NetworkValue> arguments)
        {
            foreach (Player player in PlayersList)
            {
                if (player != exceptedPlayer)
                {
                    SendRPC(player, rpcId, deliveryMethod, arguments);
                }
            }
        }
        #endregion

        #region Custom Gamemode

        /// your code here

        private void ProcessPlayerJoinedEvent(Player player)
        {
            AddGlobalChatMessage($"В игру зашел {player.nickname}");

            GiveAchievement(player, 0);
            AddChatMessage(player, $"Добро пожаловать, {player.nickname}! Для ознакомления с сервером введите /help.");
            PlayAudiostream(player, "http://bbepx.ru/games/donbass-simulator/content/donbass.mp3", isLoop: true);

            InitBulletHoles(player);
            InitDoors(player);

            InitTextdrawImage(player, "donbass");
            CreateTextdraw(player, 1, "donbass", true, new Vector2(270.29f, 84f), new Vector2(540.5819f, 169.5f), new Vector2(0, 0), new Vector2(0, 0));

            CreateDialog(player, 5);

            SetPlayerProperty(player, "health", 100.0f);
        }
        private void ProcessPlayerDeathEvent(Player player)
        {
            RespawnPlayer(player);
        }

        private void RespawnPlayer(Player player)
        {
            DeletePlayer(player);
            SetHealth(player, 100.0f);
            SetPlayerProperty(player, "health", 100.0f);
            CreateDialog(player, 5);
        }

        private bool ValidateShoot(Player player, DamageTargetType damageTarget, int targetId, float damage, Vector3 destination, float distance, Vector3 hitNormal)
        {
            return true;
        }

        private bool ValidateConnectionRequestAndKickIfNot(Player player, ConnectionRequestInfo connectionRequestInfo)
        {
            if (connectionRequestInfo.version != Config.GAME_VERSION)
            {
                new Thread(() => {
                    AddChatMessage(player, $"<color=red>Подключение невозможно!</color> Старая версия игры. Новейшая версия {Config.GAME_VERSION}. Ваша версия: {connectionRequestInfo.version}");
                    Thread.Sleep(1000);
                    KickPlayer(player);
                }).Start();

                return false;
            }

            if (connectionRequestInfo.nickname.Length < 4 || connectionRequestInfo.nickname.Length > 20)
            {
                new Thread(() => {
                    AddChatMessage(player, $"<color=red>Подключение невозможно!</color> Некорректный никнейм. Можно использовать только русские, украинские, английские буквы количеством от 4 до 20 символов.");
                    Thread.Sleep(1000);
                    KickPlayer(player);
                }).Start();

                return false;
            }

            return true;
        }

        private void PlayAudiostream(Player player, string url, bool isLoop = false, float volume = 0.1f)
        {
            List<NetworkValue> arguments = new List<NetworkValue>() { new NetworkValue(url), new NetworkValue(isLoop), new NetworkValue(volume) };
            SendRPC(player, "Rpc_AudioStream", DeliveryMethod.ReliableUnordered, arguments);
        }
        private void PlayGlobalAudiostream(string url, bool isLoop = false, float volume = 0.1f)
        {
            foreach (Player player in PlayersList)
            {
                PlayAudiostream(player, url, isLoop, volume);
            }
        }

        private void CreateDialog(Player player, int dialogId, string header, DialogType dialogType, object dialogContent)
        {
            Type dialogContentType = dialogContent.GetType();

            if (dialogContentType == typeof(string[]))
            {
                string[] castedValues = (string[])dialogContent;
                List<NetworkValue> stringValues = ProtonTypes.ConvertToNetworkValuesList(new List<string>(castedValues));

                dialogContent = stringValues;
            }
            else if (dialogContentType != typeof(string))
            {
                return;
            }

            List<NetworkValue> arguments = new List<NetworkValue>() { new NetworkValue(dialogId), new NetworkValue(header), new NetworkValue((byte)(int)dialogType), new NetworkValue(dialogContent) };

            SendRPC(player, RpcListener.GetStringHash("Rpc_Dialog"), DeliveryMethod.ReliableUnordered, arguments);
        }

        private void CreatePickup(Player player, Vector3 position, int pickupId)
        {
            List<NetworkValue> arguments = new List<NetworkValue>() { new NetworkValue(position), new NetworkValue(pickupId) };

            SendRPC(player, "Rpc_CreatePickup", DeliveryMethod.ReliableUnordered, arguments);
        }
        private void DeletePickup(Player player, int pickupId)
        {
            List<NetworkValue> arguments = new List<NetworkValue>() { new NetworkValue(pickupId) };

            SendRPC(player, "Rpc_RemovePickup", DeliveryMethod.ReliableUnordered, arguments);
        }

        private void InitTextdrawImage(Player player, string imageName)
        {
            string imagePath = $"content/{imageName}.png";

            if (File.Exists(imagePath))
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);

                List<NetworkValue> arguments = new List<NetworkValue>() { new NetworkValue(RpcListener.GetStringHash(imageName)), new NetworkValue(imageBytes) };

                SendRPC(player, "Rpc_InitTextdraw", DeliveryMethod.ReliableOrdered, arguments);
            }
        }
        private void CreateTextdraw(Player player, int textdrawId, string imageName, bool clickable, Vector2 position, Vector2 size, Vector2 minAnchor, Vector2 maxAnchor, string text = "", bool onMap = false)
        {
            List<NetworkValue> arguments = new List<NetworkValue>()
            {
                new NetworkValue(textdrawId),
                new NetworkValue(RpcListener.GetStringHash(imageName)),
                new NetworkValue(clickable),
                new NetworkValue(position),
                new NetworkValue(size),
                new NetworkValue(minAnchor),
                new NetworkValue(maxAnchor),
                new NetworkValue(text),
                new NetworkValue(onMap)
            };

            SendRPC(player, "Rpc_CreateTextdraw", DeliveryMethod.ReliableOrdered, arguments);
        }
        private void DeleteTextdraw(Player player, int textdrawId)
        {
            SendRPC(player, "Rpc_DeleteTextdraw", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(textdrawId) });
        }

        private void SetHealth(Player player, float health)
        {
            SendRPC(player, "Rpc_SetHealth", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(health) });
        }

        private void GiveAchievement(Player player, int achievementId)
        {
            SendRPC(player, "Rpc_GiveAchievement", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(achievementId) });
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
        }

        private void TeleportPlayer(Player player, Vector3 position)
        {
            SendRPC(player, "Rpc_Teleport", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(position) });
        }
        private void TeleportAllPlayers(Vector3 position)
        {
            foreach (Player player in PlayersList)
            {
                TeleportPlayer(player, position);
            }
        }

        private void CreateVehicle(Player player, string name, Vector3 position, Vector3 rotation)
        {
            SendRPC(player, "Rpc_CreateVehicle", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(name), new NetworkValue(position), new NetworkValue(rotation) });
        }
        private void CreateAirplane(Player player, string name, Vector3 position, Vector3 rotation)
        {
            SendRPC(player, "Rpc_CreateAirplane", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(name), new NetworkValue(position), new NetworkValue(rotation) });
        }

        private void SpawnPlayer(Player player, Vector3 position)
        {
            SendRPC(player, "Rpc_Spawn", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(position) });
        }
        private void DeletePlayer(Player player)
        {
            SendRPC(player, "Rpc_DeletePlayer", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { });
        }

        private void CreateObject(Player player, string objectName, Vector3 position=null, Vector3 rotation=null, int objectId=-1)
        {
            if (position == null)
            {
                position = new Vector3();
            }

            if (rotation == null)
            {
                rotation = new Vector3();
            }

            if (objectId == -1)
            {
                objectId = new Random().Next(-0x7FFFFFFF, -0x000000FF);
            }

            SendRPC(player, "Rpc_CreateBundleObject", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() {
                new NetworkValue(objectName),
                new NetworkValue(position),
                new NetworkValue(rotation),
                new NetworkValue(objectId)
            });
        }
        private void DestroyObject(Player player, int objectId)
        {
            SendRPC(player, "Rpc_DestroyBundleObject", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(objectId) });
        }
        private void CreateGlobalObject(string objectName, Vector3 position = null, Vector3 rotation = null, int objectId = -1)
        {
            foreach (Player player in PlayersList)
            {
                CreateObject(player, objectName, position, rotation, objectId);
            }
        }
        private void DestroyGlobalObject(int objectId)
        {
            foreach (Player player in PlayersList)
            {
                DestroyObject(player, objectId);
            }
        }

        private void GiveAmmo(Player player, WeaponEnum weaponType, int ammount)
        {
            SendRPC(player, "Rpc_ChangeAmmo", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(true), new NetworkValue((int)weaponType), new NetworkValue(ammount) });
        }
        private void RemoveAmmo(Player player, WeaponEnum weaponType, int ammount)
        {
            SendRPC(player, "Rpc_ChangeAmmo", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(false), new NetworkValue((int)weaponType), new NetworkValue(ammount) });
        }

        private void SpawnBombingPlane(Player player, Vector3 startPosition, Vector3 endPosition, float bombingTime=10.0f, float bombingStartTime=3.0f, float bombingEndTime=7.0f, int totalBombs=10)
        {
            SendRPC(player, "Rpc_SpawnBombingAirplane", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() {
                new NetworkValue(startPosition),
                new NetworkValue(endPosition),
                new NetworkValue(bombingTime),
                new NetworkValue(bombingStartTime),
                new NetworkValue(bombingEndTime),
                new NetworkValue(totalBombs)
            });
        }
        private void SpawnRandomBombingPlane()
        {
            if (PlayersList.Count == 0)
            {
                return;
            }

            Player player = PlayersList[new Random().Next(PlayersList.Count)];
            Vector3 startPosition = new Vector3(new Random().Next(-750, 750), new Random().Next(150, 200), new Random().Next(-750, 750));
            Vector3 endPosition = new Vector3(new Random().Next(-750, 750), new Random().Next(150, 160), new Random().Next(-750, 750));
            int totalBombs = new Random().Next(25, 30);

            SpawnBombingPlane(player, startPosition, endPosition, 10, 1, 9, totalBombs);
        }
        private void SpawnBombingPlanes()
        {
            Utils.Log("Spawn of planes has begun!");

            while (true)
            {
                Thread.Sleep(delay);
                SpawnRandomBombingPlane();
            }
        }

        private void InitDoors(Player player)
        {
            List<NetworkValue> networkDoors = ProtonTypes.ConvertToNetworkValuesList(openedDoors);

            SendRPC(player, "Rpc_InitDoors", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(networkDoors) });
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

            SendRPC(player, "Rpc_InitBulletHoles", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(protonStream.Bytes.ToArray()) });
        }

        private Dictionary<int, object[]> dialogInfo = new Dictionary<int, object[]>()
        {
            { 1, new object[] { "Помощь", DialogType.CHOOSE, new string[] { "Информация", "Список команд", "Список изменений" } } },
            { 2, new object[] { "Информация о сервере", DialogType.INFO, "Добро пожаловать на Donbass RPG. Здесь вы найдете:\nМишк фредет\nРак говна\nБебрус 228" } },
            { 3, new object[] { "Команды", DialogType.INFO, "Список команд:\n/help - диалог с помощью\n/car - спавн машины\n/plane - телепорт на полосу\n/tp - телепорт всех игроков к себе\n/object - спавн объекта\n/prate (задержка) - ставит задержку спавна самолетов\n/spawn - спавнит отправителя\n/music - показывает меню выбора музыки" } },
            { 4, new object[] { "Changelog", DialogType.INFO, "да дохуя чего так то" } },
            { 5, new object[] { "Выбор спавна", DialogType.CHOOSE, new string[] { "Куканск", "Бомбецк", "Бебрастан", "Отсо-сити", "Аэропорт", "Квартира Игоря Гофмана", "Вершина горы \"Абоба\"" } } },
            { 6, new object[] { "Выбор музыки", DialogType.INPUT, "Введите название файла музыки с сервера (пример: donbass.mp3)" } },
        };

        private void CreateDialog(Player player, int Id)
        {
            if (!dialogInfo.ContainsKey(Id))
            {
                return;
            }

            object[] currentDialogInfo = dialogInfo[Id];
            CreateDialog(player, Id, (string)currentDialogInfo[0], (DialogType)currentDialogInfo[1], currentDialogInfo[2]);
        }
        #endregion

        #region Rpc Callbacks
        public void Rpc_DialogResponse(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int dialogId = (int)arguments[0].value;
            int listItemId = (int)arguments[1].value;
            bool cancel = (bool)arguments[2].value;
            string responseText = (string)arguments[3].value;

            switch (dialogId)
            {
                case (1):

                    if (cancel)
                    {
                        return;
                    }

                    switch (listItemId)
                    {
                        case (0): // информация
                            CreateDialog(player, 2);
                            break;

                        case (1): // команды
                            CreateDialog(player, 3);
                            break;

                        case (2): // changelog
                            CreateDialog(player, 4);
                            break;
                    }

                    break;

                case (2):
                    CreateDialog(player, 1);
                    break;

                case (3):
                    CreateDialog(player, 1);
                    break;

                case (4):
                    CreateDialog(player, 1);
                    break;

                case (5):
                    if (cancel)
                    {
                        new Thread(() =>
                        {
                            AddChatMessage(player, "Ну и пошел нахуй, долбаеб.");
                            Thread.Sleep(1000);
                            KickPlayer(player);
                        }
                        ).Start();

                        return;
                    }

                    switch (listItemId)
                    {
                        //"Куканск", "Бомбецк", "Бебрастан", "Отсо-сити", "Аэропорт", "Квартира Игоря Гофмана", "Вершина горы \"Бебра\"" 

                        case (0):
                            SpawnPlayer(player, new Vector3(639.8541f, -8.36f, -756.5322f));
                            break;

                        case (1):
                            SpawnPlayer(player, new Vector3(653.5f, -8.36f, 476.9f));
                            break;

                        case (2):
                            SpawnPlayer(player, new Vector3(273.2f, 7.08f, -508.7f));
                            break;

                        case (3):
                            SpawnPlayer(player, new Vector3(-425.47f, -8.66f, 126.9f));
                            break;

                        case (4):
                            SpawnPlayer(player, new Vector3(-736f, 8f, -663.9f));
                            break;

                        case (5):
                            SpawnPlayer(player, new Vector3(852.517f, -4.07f, -741.194f));
                            break;

                        case (6):
                            SpawnPlayer(player, new Vector3(-44.38f, 140.11f, -5.22f));
                            break;
                    }

                    break;

                case (6):
                    if (cancel)
                    {
                        return;
                    }

                    AddGlobalChatMessage($"Начинаем проигрывать композицию с именем: {responseText}");
                    PlayGlobalAudiostream($"http://bbepx.ru/games/donbass-simulator/content/{responseText}", true, 0.2f);
                    break;
            }
        }

        public void Rpc_UpdatePosition(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            Vector3 position = (Vector3)arguments[0].value;

            SetPlayerProperty(player, "position", position);
        }

        public void Rpc_GetPickup(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int pickupId = (int)arguments[0].value;
        }

        public void Rpc_OpenDoor(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int doorId = (int)arguments[0].value;
            bool open = (bool)arguments[1].value;

            if (open)
            {
                if (!openedDoors.Contains(doorId))
                {
                    openedDoors.Add(doorId);
                }
            }
            else
            {
                if (openedDoors.Contains(doorId))
                {
                    openedDoors.Remove(doorId);
                }
            }

            if (doorId == -1098202634) // hofman house
            {
                GiveAchievement(player, 5);
            }
        }

        public void Rpc_ClickTextdraw(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int textdrawId = (int)arguments[0].value;
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

            if (!ValidateShoot(player, damageTarget, bulletTargetId, damage, destination, distance, hitNormal))
            {
                return;
            }

            if (damageTarget != DamageTargetType.AIR)
            {
                ShootHole shootHole = new ShootHole() { position = destination, normal = hitNormal };
                shootHoles.Add(shootHole);

                if (shootHoles.Count > Config.MAX_BULLET_HOLES)
                {
                    shootHoles.RemoveAt(0);
                }
            }

            SendGlobalRPCExcept(player, "Rpc_Shoot", DeliveryMethod.ReliableUnordered, arguments);

            if (damageTarget == DamageTargetType.PLAYER && bulletTargetId != 0)
            {
                Player targetPlayer = GetPlayerById(bulletTargetId);
                if (targetPlayer != null)
                {
                    float newHealth = (float)GetPlayerProperty(targetPlayer, "health") - damage;
                    if (newHealth > 0f)
                    {
                        SetPlayerProperty(targetPlayer, "health", newHealth);
                    }
                    else
                    {
                        ProcessPlayerDeathEvent(targetPlayer);

                        AddGlobalChatMessage($"Игрок {targetPlayer.nickname} был убит игроком {player.nickname}. Ну они и дурачки");
                    }
                }
            }
        }

        public void Rpc_ChangeWeapon(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int weaponId = (int)arguments[0].value;
            WeaponEnum weaponType = (WeaponEnum)Enum.ToObject(typeof(WeaponEnum), weaponId);

            SendGlobalRPCExcept(player, "Rpc_ChangeWeapon", DeliveryMethod.ReliableUnordered, new List<NetworkValue>() { new NetworkValue(player.Id), new NetworkValue(weaponId) });
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
                SetPlayerProperty(player, "health", newHealth);
            }
            else
            {
                ProcessPlayerDeathEvent(player);

                AddGlobalChatMessage($"Игрок {player.nickname} умер по неизвестной причине. Вечная память");
            }
        }

        public void Rpc_ClickEntity(Player player, int targetId, List<NetworkValue> arguments, DeliveryMethod deliveryMethod)
        {
            int entityId = (int)arguments[0].value;

            DestroyGlobalObject(entityId);

            foreach (WeaponEnum weaponType in Enum.GetValues<WeaponEnum>())
            {
                GiveAmmo(player, weaponType, 1000);
            }
        }
        #endregion
    }
}
