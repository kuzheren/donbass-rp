using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private object GetSVar(string propertyName)
        {
            if (ServerProperties.ContainsKey(propertyName))
            {
                return ServerProperties[propertyName];
            }
            return null;
        }
        private void SetSVar(string propertyName, object property)
        {
            if (property == null)
            {
                ServerProperties.Remove(propertyName);
                return;
            }
            ServerProperties[propertyName] = property;
        }

        private object GetSVar(SVar propertyName)
        {
            return GetSVar(propertyName.ToString());
        }
        private void SetSVar(SVar propertyName, object property)
        {
            SetSVar(propertyName.ToString(), property);
        }
    }
}
