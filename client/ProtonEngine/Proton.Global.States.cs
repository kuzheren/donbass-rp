using System.Collections;
using System.Collections.Generic;

namespace Proton
{
    public enum GameState
    {
        Disconnected,
        ConnectionRequest,
        Connected
    }
    public static class ProtonGlobalStates
    {
        public static GameState connectionState = GameState.Disconnected;
    }
}