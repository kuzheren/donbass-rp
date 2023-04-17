using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Proton.Utils
{
    /// <summary>
    /// �������������� ���������� ������� � ����� ��� ������� ������������.
    /// </summary>
    public static class Identificators
    {
        public const byte PACKET = 30;
        public const byte RPC = 31;

        public const byte NULL = 0;
        public const byte BOOL = 1;
        public const byte BYTE = 2;
        public const byte UINT16 = 3;
        public const byte INT16 = 4;
        public const byte UINT32 = 5;
        public const byte INT32 = 6;
        public const byte UINT64 = 7;
        public const byte INT64 = 8;
        public const byte FLOAT = 9;
        public const byte DOUBLE = 10;
        public const byte STRING = 11;
        public const byte VECTOR3 = 12;
        public const byte VECTOR2 = 13;
        public const byte QUATERNION = 14;
        public const byte LIST = 15;
        public const byte DICTIONARY = 16;
        public const byte BYTEARRAY = 17;
        public const byte STRUCT = 18;
        public const byte NETWORK_VALUE = 19;
    }

    /// <summary>
    /// �������������� �������.
    /// </summary>
    public enum PacketId
    {
        CONNECTION_INFO = 1,
        CONNECTION_ACCEPTED = 2,
        PLAYERS_LIST = 3,
        PLAYER_CLASS = 4,
        PLAYER_CLASS_REMOVE = 5,
        CHAT = 6
    }

    /// <summary>
    /// �������������� ����� ��������� RPC.
    /// </summary>
    public enum RPCTarget
    {
        SERVER = 1,
        ALL = 2,
        OTHERS = 3
    }
}
