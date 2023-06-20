using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private void SetBombingState(bool state)
        {
            SetSVar(SVar.IsBombing, state);
        }
        private bool GetBombingState()
        {
            object bombingStateObject = GetSVar(SVar.IsBombing);
            if (bombingStateObject == null)
            {
                return false;
            }

            return (bool)bombingStateObject;
        }

        private void SetServerOpenState(bool state)
        {
            SetSVar(SVar.IsServerOpened, state);
        }
        private bool GetServerOpenState()
        {
            object bombingStateObject = GetSVar(SVar.IsServerOpened);
            if (bombingStateObject == null)
            {
                return true;
            }

            return (bool)bombingStateObject;
        }
    }
}
