using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Proton.Utils
{
    /// <summary>
    /// Интерфейс для сериализации структур
    /// </summary>
    public interface IProtonSerializable
    {
        public void SerializeToStream(ProtonStream stream);
        public void DeserializeFromStream(ProtonStream stream);
    }

    /// <summary>
    /// Сетевое значение. Поддерживает все простые типы данных,
    /// байтовые массивы, списки других NetworkValue, словари с NetworkValue,
    /// NetworkStructure, ProtonStream и даже другие NetworkValue.
    /// Основа сериализации ProtonEngine.
    /// </summary>
    public class NetworkValue
    {
        public Type type;
        public byte typeId;
        public object value;

        public NetworkValue(byte typeId, object value)
        {
            this.type = value.GetType();
            this.typeId = typeId;
            this.value = value;
        }
        public NetworkValue(object value)
        {
            this.type = value.GetType();
            this.typeId = ProtonTypes.GetTypeId(value.GetType());
            this.value = value;
        }
    }

    /// <summary>
    /// Сетевая структура. Содержит структуру с IProtonSerializable и ее имя.
    /// </summary>
    public class NetworkStructure
    {
        public Type type;
        public string structName;
        public IProtonSerializable structure;

        public NetworkStructure(IProtonSerializable structure)
        {
            this.type = structure.GetType();
            this.structName = structure.GetType().Name;
            this.structure = structure;
        }
    }

    /// <summary>
    /// Основной класс сериализации. Поддерживает запись NetworkValue,
    /// простых типов данных, байтовых массивов, основных типов данных Unity
    /// и даже других ProtonStream.
    /// </summary>
    public class ProtonStream
    {
        public List<byte> Bytes = new List<byte>();
        public int ReadOffset = 0;

        public ProtonStream() { }
        public ProtonStream(byte[] Bytes) { this.Bytes = new List<byte>(Bytes); }
        public ProtonStream(List<byte> Bytes) { this.Bytes = Bytes; }

        public void WriteByte(byte value)
        {
            Bytes.Add(value);
        }
        public void WriteBytes(byte[] values)
        {
            foreach (byte value in values)
            {
                WriteByte(value);
            }
        }
        public void Write(object value)
        {
            Type type = value.GetType();

            if (type == typeof(bool))
            {
                WriteBool((bool)value);
            }
            else if (type == typeof(byte))
            {
                WriteByte((byte)value);
            }
            else if (type == typeof(UInt16))
            {
                WriteUInt16((UInt16)value);
            }
            else if (type == typeof(Int16))
            {
                WriteInt16((Int16)value);
            }
            else if (type == typeof(UInt32))
            {
                WriteUInt32((UInt32)value);
            }
            else if (type == typeof(Int32))
            {
                WriteInt32((Int32)value);
            }
            else if (type == typeof(UInt64))
            {
                WriteUInt64((UInt64)value);
            }
            else if (type == typeof(Int64))
            {
                WriteInt64((Int64)value);
            }
            else if (type == typeof(float))
            {
                WriteFloat((float)value);
            }
            else if (type == typeof(double))
            {
                WriteDouble((double)value);
            }
            else if (type == typeof(string))
            {
                WriteString((string)value);
            }
            else if (type == typeof(Vector3))
            {
                WriteVector3((Vector3)value);
            }
            else if (type == typeof(Vector2))
            {
                WriteVector2((Vector2)value);
            }
            else if (type == typeof(Quaternion))
            {
                WriteQuaternion((Quaternion)value);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type valueType = type.GetGenericArguments()[0];
                if (valueType == typeof(NetworkValue))
                {
                    WriteList((List<NetworkValue>)value);
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = type.GetGenericArguments()[0];
                Type valueType = type.GetGenericArguments()[1];
                if (keyType == typeof(string) && valueType == typeof(NetworkValue))
                {
                    WriteDictionary((Dictionary<string, NetworkValue>)value);
                }
            }
            else if (type == typeof(NetworkValue))
            {
                WriteNetworkValue((NetworkValue)value);
            }
            else if (type == typeof(byte[]))
            {
                WriteBytearray((byte[])value);
            }
            else if (type == typeof(ProtonStream))
            {
                WriteProtonStream((ProtonStream)value);
            }
            else if (type == typeof(NetworkStructure))
            {
                WriteNetworkStructure((NetworkStructure)value);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        public void WriteBool(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }
        public void WriteUInt16(UInt16 value) // ushort
        {
            WriteBytes(BitConverter.GetBytes(value));
        }
        public void WriteInt16(Int16 value) // short
        {
            WriteBytes(BitConverter.GetBytes(value));
        }
        public void WriteUInt32(UInt32 value) // uint
        {
            WriteBytes(BitConverter.GetBytes(value));
        }
        public void WriteInt32(Int32 value) // int
        {
            WriteBytes(BitConverter.GetBytes(value));
        }
        public void WriteUInt64(UInt64 value) // ulong
        {
            WriteBytes(BitConverter.GetBytes(value));
        }
        public void WriteInt64(Int64 value) // long
        {
            WriteBytes(BitConverter.GetBytes(value));
        }
        public void WriteFloat(float value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }
        public void WriteDouble(double value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }
        public void WriteString(String value)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(value);
            ushort length = (ushort)stringBytes.Length;

            WriteUInt16(length);
            for (int i = 0; i < length; i++)
            {
                WriteByte(stringBytes[i]);
            }
        }
        public void WriteVector3(Vector3 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
        }
        public void WriteVector2(Vector2 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
        }
        public void WriteQuaternion(Quaternion value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
            WriteFloat(value.w);
        }

        public void WriteNetworkValue(NetworkValue value)
        {
            WriteByte(value.typeId);
            Write(value.value);
        }
        public void WriteList(List<NetworkValue> values)
        {
            WriteUInt32((uint)values.Count);
            foreach (NetworkValue value in values)
            {
                WriteNetworkValue(value);
            }
        }
        public void WriteDictionary(Dictionary<string, NetworkValue> values)
        {
            WriteUInt32((uint)values.Count);
            foreach (string key in values.Keys)
            {
                WriteString(key);
                WriteNetworkValue(values[key]);
            }
        }
        public void WriteBytearray(byte[] values)
        {
            WriteUInt32((uint)values.Length);
            WriteBytes(values);
        }
        public void WriteStructure(IProtonSerializable value)
        {
            value.SerializeToStream(this);
        }
        public void WriteNetworkStructure(NetworkStructure value)
        {
            WriteString(value.structName);
            WriteStructure(value.structure);
        }
        public void WriteProtonStream(ProtonStream value)
        {
            WriteBytes(value.Bytes.ToArray());
        }

        public byte ReadByte()
        {
            ReadOffset++;
            return Bytes[ReadOffset - 1]; // самое ебучее место в мире сука
        }
        public byte[] ReadBytes(int ammount)
        {
            if (ammount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return ReadBytes((uint)ammount);
        }
        public byte[] ReadBytes(uint ammount)
        {
            List<byte> result = new List<byte>();
            for (int i = 0; i < (int)ammount; i++)
            {
                result.Add(ReadByte());
            }
            return result.ToArray();
        }
        public T Read<T>()
        {
            Type type = typeof(T);

            if (type == typeof(bool))
            {
                return (T)(object)ReadBool();
            }
            else if (type == typeof(byte))
            {
                return (T)(object)ReadByte();
            }
            else if (type == typeof(UInt16))
            {
                return (T)(object)ReadUInt16();
            }
            else if (type == typeof(Int16))
            {
                return (T)(object)ReadInt16();
            }
            else if (type == typeof(UInt32))
            {
                return (T)(object)ReadUInt32();
            }
            else if (type == typeof(Int32))
            {
                return (T)(object)ReadInt32();
            }
            else if (type == typeof(UInt64))
            {
                return (T)(object)ReadUInt64();
            }
            else if (type == typeof(Int64))
            {
                return (T)(object)ReadInt64();
            }
            else if (type == typeof(float))
            {
                return (T)(object)ReadFloat();
            }
            else if (type == typeof(double))
            {
                return (T)(object)ReadDouble();
            }
            else if (type == typeof(string))
            {
                return (T)(object)ReadString();
            }
            else if (type == typeof(Vector3))
            {
                return (T)(object)ReadVector3();
            }
            else if (type == typeof(Vector2))
            {
                return (T)(object)ReadVector2();
            }
            else if (type == typeof(Quaternion))
            {
                return (T)(object)ReadQuaternion();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type valueType = type.GetGenericArguments()[0];
                if (valueType == typeof(NetworkValue))
                {
                    return (T)(object)ReadList();
                }
                return default(T);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = type.GetGenericArguments()[0];
                Type valueType = type.GetGenericArguments()[1];
                if (keyType == typeof(string) && valueType == typeof(NetworkValue))
                {
                    return (T)(object)ReadDictionary();
                }
                return default(T);
            }
            else if (type == typeof(byte[]))
            {
                return (T)(object)ReadBytearray();
            }
            else if (type == typeof(NetworkValue))
            {
                return (T)(object)ReadNetworkValue();
            }
            else if (type == typeof(NetworkStructure))
            {
                return (T)(object)ReadNetworkStructure();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        public object Read(Type type)
        {
            if (type == typeof(bool))
            {
                return Read<bool>();
            }
            else if (type == typeof(byte))
            {
                return Read<byte>();
            }
            else if (type == typeof(UInt16))
            {
                return Read<UInt16>();
            }
            else if (type == typeof(Int16))
            {
                return Read<Int16>();
            }
            else if (type == typeof(UInt32))
            {
                return Read<UInt32>();
            }
            else if (type == typeof(Int32))
            {
                return Read<Int32>();
            }
            else if (type == typeof(UInt64))
            {
                return Read<UInt64>();
            }
            else if (type == typeof(Int64))
            {
                return Read<Int64>();
            }
            else if (type == typeof(float))
            {
                return Read<float>();
            }
            else if (type == typeof(double))
            {
                return Read<double>();
            }
            else if (type == typeof(string))
            {
                return Read<string>();
            }
            else if (type == typeof(Vector3))
            {
                return Read<Vector3>();
            }
            else if (type == typeof(Vector2))
            {
                return Read<Vector2>();
            }
            else if (type == typeof(Quaternion))
            {
                return Read<Quaternion>();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type valueType = type.GetGenericArguments()[0];
                if (valueType == typeof(NetworkValue))
                {
                    return Read<List<NetworkValue>>();
                }
                return null;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = type.GetGenericArguments()[0];
                Type valueType = type.GetGenericArguments()[1];
                if (keyType == typeof(string) && valueType == typeof(NetworkValue))
                {
                    return Read<Dictionary<string, NetworkValue>>();
                }
                return null;
            }
            else if (type == typeof(byte[]))
            {
                return Read<byte[]>();
            }
            else if (type == typeof(NetworkValue))
            {
                return Read<NetworkValue>();
            }
            else if (type == typeof(NetworkStructure))
            {
                return Read<NetworkStructure>();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        public bool ReadBool()
        {
            return ReadByte() != 0;
        }
        public UInt16 ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadBytes(2), 0);
        }
        public Int16 ReadInt16()
        {
            return BitConverter.ToInt16(ReadBytes(2), 0);
        }
        public UInt32 ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBytes(4), 0);
        }
        public Int32 ReadInt32()
        {
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }
        public UInt64 ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadBytes(8), 0);
        }
        public Int64 ReadInt64()
        {
            return BitConverter.ToInt64(ReadBytes(8), 0);
        }
        public float ReadFloat()
        {
            return BitConverter.ToSingle(ReadBytes(4), 0);
        }
        public double ReadDouble()
        {
            return BitConverter.ToDouble(ReadBytes(8), 0);
        }
        public string ReadString()
        {
            ushort length = ReadUInt16();
            return Encoding.UTF8.GetString(ReadBytes(length));
        }
        public Vector3 ReadVector3()
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }
        public Vector2 ReadVector2()
        {
            return new Vector2(ReadFloat(), ReadFloat());
        }
        public Quaternion ReadQuaternion()
        {
            return new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
        }
        public NetworkValue ReadNetworkValue()
        {
            byte type = ReadByte();
            Type valueType = ProtonTypes.GetTypeById(type);

            if (valueType == null)
            {
                return null;
            }

            object value = Read(valueType);

            return new NetworkValue(type, value);
        }
        public List<NetworkValue> ReadList()
        {
            uint listCount = ReadUInt32();
            List<NetworkValue> result = new List<NetworkValue>();
            for (uint i = 0; i < listCount; i++)
            {
                result.Add(ReadNetworkValue());
            }
            return result;
        }
        public Dictionary<string, NetworkValue> ReadDictionary()
        {
            uint dictionaryCount = ReadUInt32();
            Dictionary<string, NetworkValue> result = new Dictionary<string, NetworkValue>();
            for (uint i = 0; i < dictionaryCount; i++)
            {
                result[ReadString()] = ReadNetworkValue();
            }
            return result;
        }
        public byte[] ReadBytearray()
        {
            uint arrayLength = ReadUInt32();
            return ReadBytes(arrayLength);
        }
        public IProtonSerializable ReadStructure(Type valueType)
        {
            IProtonSerializable structure = (IProtonSerializable)Activator.CreateInstance(valueType);
            structure.DeserializeFromStream(this);

            return structure;
        }
        public NetworkStructure ReadNetworkStructure()
        {
            string structureName = ReadString();

            Type structureType = Type.GetType("Proton.Utils." + structureName);
            IProtonSerializable structure = ReadStructure(structureType);

            return new NetworkStructure(structure);
        }
    }

    /// <summary>
    /// Класс для конвертации сетевых идентификаторов типов в типы и обратно.
    /// </summary>
    public static class ProtonTypes
    {
        public static byte GetTypeId(Type type)
        {
            if (type == typeof(bool))
            {
                return Identificators.BOOL;
            }
            else if (type == typeof(byte))
            {
                return Identificators.BYTE;
            }
            else if (type == typeof(UInt16))
            {
                return Identificators.UINT16;
            }
            else if (type == typeof(Int16))
            {
                return Identificators.INT16;
            }
            else if (type == typeof(UInt32))
            {
                return Identificators.UINT32;
            }
            else if (type == typeof(Int32))
            {
                return Identificators.INT32;
            }
            else if (type == typeof(UInt64))
            {
                return Identificators.UINT64;
            }
            else if (type == typeof(Int64))
            {
                return Identificators.INT64;
            }
            else if (type == typeof(float))
            {
                return Identificators.FLOAT;
            }
            else if (type == typeof(double))
            {
                return Identificators.FLOAT;
            }
            else if (type == typeof(string))
            {
                return Identificators.STRING;
            }
            else if (type == typeof(Vector3))
            {
                return Identificators.VECTOR3;
            }
            else if (type == typeof(Vector2))
            {
                return Identificators.VECTOR2;
            }
            else if (type == typeof(Quaternion))
            {
                return Identificators.QUATERNION;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type valueType = type.GetGenericArguments()[0];
                if (valueType == typeof(NetworkValue))
                {
                    return Identificators.LIST;
                }
                return Identificators.NULL;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = type.GetGenericArguments()[0];
                Type valueType = type.GetGenericArguments()[1];
                if (keyType == typeof(string) && valueType == typeof(NetworkValue))
                {
                    return Identificators.DICTIONARY;
                }
                return Identificators.NULL;
            }
            else if (type == typeof(byte[]))
            {
                return Identificators.BYTEARRAY;
            }
            else if (type == typeof(NetworkStructure))
            {
                return Identificators.STRUCT;
            }
            else if (type == typeof(NetworkValue))
            {
                return Identificators.NETWORK_VALUE;
            }

            return Identificators.NULL;
        }
        public static Type GetTypeById(byte id)
        {
            switch (id)
            {
                case Identificators.NULL:
                    return null;

                case Identificators.BOOL:
                    return typeof(bool);

                case Identificators.BYTE:
                    return typeof(byte);

                case Identificators.UINT16:
                    return typeof(UInt16);

                case Identificators.INT16:
                    return typeof(Int16);

                case Identificators.UINT32:
                    return typeof(UInt32);

                case Identificators.INT32:
                    return typeof(Int32);

                case Identificators.UINT64:
                    return typeof(UInt64);

                case Identificators.INT64:
                    return typeof(Int64);

                case Identificators.FLOAT:
                    return typeof(float);

                case Identificators.DOUBLE:
                    return typeof(double);

                case Identificators.STRING:
                    return typeof(string);

                case Identificators.VECTOR3:
                    return typeof(Vector3);

                case Identificators.VECTOR2:
                    return typeof(Vector2);

                case Identificators.QUATERNION:
                    return typeof(Quaternion);

                case Identificators.LIST:
                    return typeof(List<NetworkValue>);

                case Identificators.DICTIONARY:
                    return typeof(Dictionary<string, NetworkValue>);

                case Identificators.BYTEARRAY:
                    return typeof(byte[]);

                case Identificators.STRUCT:
                    return typeof(NetworkStructure);

                case Identificators.NETWORK_VALUE:
                    return typeof(NetworkValue);

                default:
                    return null;
            }
        }
        public static List<NetworkValue> ConvertToNetworkValuesList<T>(List<T> values)
        {
            List<NetworkValue> networkValues = new List<NetworkValue>();
            foreach (T value in values)
            {
                if (value is IProtonSerializable)
                {
                    networkValues.Add(new NetworkValue(new NetworkStructure((IProtonSerializable)value)));
                }
                else
                {
                    networkValues.Add(new NetworkValue(value));
                }
            }
            return networkValues;
        }
    }

    /// <summary>
    /// Простой сериализатор структур и прочих значений, использующий ProtonStream.
    /// </summary>
    public class ProtonPacketSerializer
    {
        public ProtonStream stream = new ProtonStream();

        public ProtonPacketSerializer(int packetId)
        {
            stream.Write(packetId);
        }
        public ProtonPacketSerializer(int packetId, IProtonSerializable packet)
        {
            stream.Write(packetId);
            stream.WriteStructure(packet);
        }
        public ProtonPacketSerializer(int packetId, object[] values)
        {
            stream.Write(packetId);
            foreach (object value in values)
            {
                stream.Write(value);
            }
        }
        public ProtonPacketSerializer(int packetId, List<object> values)
        {
            stream.Write(packetId);
            foreach (object value in values)
            {
                stream.Write(value);
            }
        }
        public ProtonPacketSerializer(object[] values)
        {
            foreach (object value in values)
            {
                stream.Write(value);
            }
        }
        public ProtonPacketSerializer(List<object> values)
        {
            foreach (object value in values)
            {
                stream.Write(value);
            }
        }
    }

    /// <summary>
    /// Простой десериализатор структур.
    /// </summary>
    public class ProtonPacketDeserializer
    {
        public IProtonSerializable structure;

        public ProtonPacketDeserializer(ProtonStream stream, Type structureType)
        {
            structure = stream.ReadStructure(structureType);
        }
    }
}