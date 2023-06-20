using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
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
        private Player GetPlayerByPeer(NetPeer peer)
        {
            foreach (Player player in PlayersList)
            {
                if (player.playerPeer == peer)
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

        private void RespawnPlayer(Player player)
        {
            DeletePlayer(player);
            SetPlayerHealth(player, 100.0f);
            CreateDialog(player, DialogID.SpawnChoose);
        }

        private bool ValidateConnectionRequestAndKickIfNot(Player player, ConnectionRequestInfo connectionRequestInfo)
        {
            if (!GetServerOpenState())
            {
                new Thread(() => {
                    Thread.Sleep(1000);
                    AddChatMessage(player, $"Подключение невозможно! Сервер временно закрыт.");
                    Thread.Sleep(1000);
                    KickPlayer(player);
                }).Start();

                return false;
            }

            if (player.nickname == "GOOGLE")
            {
                AddChatMessage(player, "Ваш аккаунт находится в тестовом режиме. Проверка версии отключена");
            }
            else
            {
                string[] supportedVersions = Config.GAME_VERSIONS.Split(';');

                if (!Config.GAME_VERSIONS.Contains(connectionRequestInfo.version))
                {
                    new Thread(() => {
                        Thread.Sleep(1000);
                        AddChatMessage(player, $"Подключение невозможно! Старая версия игры: {connectionRequestInfo.version}. Перейдите на страницу игры в GooglePlay чтобы обновиться.");
                        Thread.Sleep(1000);
                        KickPlayer(player);
                    }).Start();

                    return false;
                }

                if (connectionRequestInfo.nickname.Length < 4 || connectionRequestInfo.nickname.Length > 20 || connectionRequestInfo.nickname.Contains(' '))
                {
                    new Thread(() => {
                        Thread.Sleep(1000);
                        AddChatMessage(player, $"Подключение невозможно! Некорректный никнейм. Можно использовать только русские, украинские, английские буквы количеством от 4 до 20 символов.");
                        Thread.Sleep(1000);
                        KickPlayer(player);
                    }).Start();

                    return false;
                }

                foreach (Player serverPlayer in PlayersList)
                {
                    if (player.nickname == serverPlayer.nickname && player != serverPlayer)
                    {
                        new Thread(() => {
                            AddChatMessage(player, $"Подключение невозможно! Никнейм занят.");
                            Thread.Sleep(500);
                            KickPlayer(player);
                        }).Start();

                        return false;
                    }
                }

                if (PlayersList.Count > Config.MAX_PLAYERS)
                {
                    new Thread(() => {
                        AddChatMessage(player, $"Подключение невозможно! Сервер переполнен.");
                        Thread.Sleep(500);
                        KickPlayer(player);
                    }).Start();

                    return false;
                }
            }

            Dictionary<string, NetworkValue> customLoginInfo = connectionRequestInfo.customData;
            bool isAndroid = (bool)customLoginInfo["IsAndroid"].value;
            string OS = (string)customLoginInfo["OS"].value;
            bool adSupport = (bool)customLoginInfo["AdSupport"].value;
            string token = (string)customLoginInfo["Token"].value;

            SetAndroidPlayerState(player, isAndroid);
            SetPlayerToken(player, token);

            ProcessPlayerJoinedEvent(player);

            return true;
        }

        private void PlayAudiostream(Player player, string url, bool isLoop = false, float volume = 0.1f)
        {
            if (!url.StartsWith("http"))
            {
                url = "http://bbepx.ru/games/donbass-simulator/content/" + url;
            }

            List<NetworkValue> arguments = new List<NetworkValue>() { new NetworkValue(url), new NetworkValue(isLoop), new NetworkValue(volume) };
            SendRPC(player, "Rpc_AudioStream", DeliveryMethod.ReliableOrdered, arguments);
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

            SendRPC(player, "Rpc_Dialog", DeliveryMethod.ReliableOrdered, arguments);
        }

        private void CreatePickup(Player player, int Id, bool big=false, bool onlyVehicle=false)
        {
            CreatePickup(player, pickupInfo[Id], Id, big, onlyVehicle);
        }
        private void CreatePickup(Player player, Vector3 position, int pickupId, bool big=false, bool onlyVehicle=false)
        {
            List<NetworkValue> arguments = new List<NetworkValue>() { new NetworkValue(position), new NetworkValue(pickupId), new NetworkValue(big), new NetworkValue(onlyVehicle) };

            SendRPC(player, "Rpc_CreatePickup", DeliveryMethod.ReliableOrdered, arguments);
        }
        private void DeletePickup(Player player, int pickupId)
        {
            List<NetworkValue> arguments = new List<NetworkValue>() { new NetworkValue(pickupId) };

            SendRPC(player, "Rpc_RemovePickup", DeliveryMethod.ReliableOrdered, arguments);
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
        private void CreateTextdraw(Player player, int textdrawId, string imageName, bool clickable, Vector2 position, Vector2 size, Vector2 minAnchor, Vector2 maxAnchor, string text = "", bool onMap = false, bool resizeText = false)
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
                new NetworkValue(onMap),
                new NetworkValue(resizeText)
            };

            SendRPC(player, "Rpc_CreateTextdraw", DeliveryMethod.ReliableOrdered, arguments);
        }
        private void DeleteTextdraw(Player player, int textdrawId)
        {
            SendRPC(player, "Rpc_DeleteTextdraw", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(textdrawId) });
        }
        private void DeleteTextdraw(Player player, TextdrawID textdrawId)
        {
            DeleteTextdraw(player, (int)textdrawId);
        }

        private void SendSetHealth(Player player, float health)
        {
            SendRPC(player, "Rpc_SetHealth", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(health) });
        }

        private void GiveAchievement(Player player, int achievementId)
        {
            SendRPC(player, "Rpc_GiveAchievement", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(achievementId) });
        }

        private void TeleportPlayer(Player player, Vector3 position)
        {
            SendRPC(player, "Rpc_Teleport", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(position) });
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
            SendRPC(player, "Rpc_CreateVehicle", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(name), new NetworkValue(position), new NetworkValue(rotation) });
        }
        private void CreateAirplane(Player player, string name, Vector3 position, Vector3 rotation)
        {
            SendRPC(player, "Rpc_CreateAirplane", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(name), new NetworkValue(position), new NetworkValue(rotation) });
        }

        private void SpawnPlayer(Player player, Vector3 position)
        {
            SendRPC(player, "Rpc_Spawn", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(position) });

            if (IsPlayerLogged(player))
            {
                GivePVPWeapons(player);
            }
        }
        private void DeletePlayer(Player player)
        {
            SendRPC(player, "Rpc_DeletePlayer", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { });
        }

        private void CreateObject(Player player, string objectName, Vector3 position = null, Vector3 rotation = null, int objectId = -1)
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

            SendRPC(player, "Rpc_CreateBundleObject", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() {
                new NetworkValue(objectName),
                new NetworkValue(position),
                new NetworkValue(rotation),
                new NetworkValue(objectId)
            });
        }
        private void DestroyObject(Player player, int objectId)
        {
            SendRPC(player, "Rpc_DestroyBundleObject", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(objectId) });
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
            SendRPC(player, "Rpc_ChangeAmmo", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(true), new NetworkValue((int)weaponType), new NetworkValue(ammount) });
        }
        private void RemoveAmmo(Player player, WeaponEnum weaponType, int ammount)
        {
            SendRPC(player, "Rpc_ChangeAmmo", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(false), new NetworkValue((int)weaponType), new NetworkValue(ammount) });
        }

        private void SpawnBombingPlane(Player player, Vector3 startPosition, Vector3 endPosition, float bombingTime = 10.0f, float bombingStartTime = 3.0f, float bombingEndTime = 7.0f, int totalBombs = 10)
        {
            SendRPC(player, "Rpc_SpawnBombingAirplane", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() {
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
            Vector3 playerPos = GetPlayerPosition(player);
            if (playerPos == null)
            {
                return;
            }

            float pX = playerPos.x;
            float pY = playerPos.y;
            float pZ = playerPos.z;

            Vector3 startPosition = new Vector3(new Random().NextSingle(pX - 250f, pX + 250f), new Random().NextSingle(pY + 100, pY + 150), new Random().NextSingle(pZ - 250f, pZ + 250f));
            Vector3 endPosition = new Vector3(new Random().NextSingle(pX - 250f, pX + 250f), new Random().NextSingle(pY + 100, pY + 150), new Random().NextSingle(pZ - 250f, pZ + 250f)); ;
            int totalBombs = new Random().Next(5, 10);

            SpawnBombingPlane(player, startPosition, endPosition, 5, 1, 4, totalBombs);
        }

        private void OpenLink(Player player, string link)
        {
            SendRPC(player, "Rpc_OpenLink", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(link) });
        }

        private void InstantiatePhotonBundleObject(Player player, int objectUniqueId, string gameobjectName, Vector3 position, Vector3 rotation = null)
        {
            if (rotation == null)
            {
                rotation = new Vector3();
            }

            SendRPC(player, "Rpc_InstantiatePhotonBundleObject", DeliveryMethod.ReliableOrdered, new List<NetworkValue>()
            {
                new NetworkValue(objectUniqueId),
                new NetworkValue(gameobjectName),
                new NetworkValue(position),
                new NetworkValue(rotation)
            });
        }
        private void DestroyPhotonBundleObject(Player player, int objectUniqueId)
        {
            SendRPC(player, "Rpc_DestroyPhotonBundleObject", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(objectUniqueId) });
        }

        private void ShowVideoAd(Player player)
        {
            SendRPC(player, "Rpc_ShowAd", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(true) });
        }
        private void ShowBannerAd(Player player)
        {
            SendRPC(player, "Rpc_ShowAd", DeliveryMethod.ReliableOrdered, new List<NetworkValue>() { new NetworkValue(false) });
        }

        private void CreateDialog(Player player, int Id)
        {
            if (!staticDialogInfo.ContainsKey(Id))
            {
                return;
            }

            object[] currentDialogInfo = staticDialogInfo[Id];
            CreateDialog(player, Id, (string)currentDialogInfo[0], (DialogType)currentDialogInfo[1], currentDialogInfo[2]);
        }
        private void CreateDialog(Player player, int Id, params object[] formatArgs)
        {
            if (!staticDialogInfo.ContainsKey(Id))
            {
                return;
            }

            object[] currentDialogInfo = staticDialogInfo[Id];
            CreateDialog(player, Id, (string)currentDialogInfo[0], (DialogType)currentDialogInfo[1], String.Format(((string)currentDialogInfo[2]), formatArgs));
        }
        private void CreateDialog(Player player, DialogID Id)
        {
            CreateDialog(player, (int)Id);
        }
        private void CreateDialog(Player player, DialogID Id, params object[] formatArgs)
        {
            CreateDialog(player, (int)Id, formatArgs);
        }

        private Vector2 ConvertWorldPositionToMap(Vector3 worldPosition)
        {
            return new Vector2(worldPosition.z / 2, -worldPosition.x / 2);
        }
    }
}
