using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private object GetPVar(Player player, string propertyName)
        {
            if (player == null)
            {
                return null;
            }

            if (player.properties.ContainsKey(propertyName))
            {
                return player.properties[propertyName];
            }
            return null;
        }
        private void SetPVar(Player player, string propertyName, object property)
        {
            if (property == null)
            {
                player.properties.Remove(propertyName);
                return;
            }
            player.properties[propertyName] = property;
        }

        private object GetPVar(Player player, PVar propertyName)
        {
            return GetPVar(player, propertyName.ToString());
        }
        private void SetPVar(Player player, PVar propertyName, object property)
        {
            SetPVar(player, propertyName.ToString(), property);
        }
    }
}
