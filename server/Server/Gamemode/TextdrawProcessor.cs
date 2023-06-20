using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private void ProcessClickTextdraw(Player player, int textdrawId)
        {
            TextdrawID textdrawID = (TextdrawID)Enum.ToObject(typeof(TextdrawID), textdrawId);

            switch (textdrawID)
            {
                case (TextdrawID.Exception):
                    {
                        DeleteTextdraw(player, TextdrawID.Exception);

                        break;
                    }
            }
        }
    }
}
