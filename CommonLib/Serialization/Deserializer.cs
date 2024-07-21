using CommonLib.Extensions;
using CommonLib.Utilities;
using CommonLib.Serialization.Buffers;
using CommonLib.Serialization.Pooling;

using System;
using System.Net;
using System.Reflection;
using System.Collections.Generic;
using MessagePack;

namespace CommonLib.Serialization
{
    public class Deserializer
    {
        public DeserializerBuffer Buffer { get; }

        public Deserializer()
            => Buffer = new DeserializerBuffer();

        public byte GetByte()
            => Buffer.Take(1)[0];

        public byte[] GetBytes(int count)
            => Buffer.Take(count);

        public byte[] GetBytes()
            => Buffer.Take(Buffer.Take(4).ToInt());

        public short GetInt16()
            => Buffer.Take(2).ToShort();

        public ushort GetUInt16()
            => Buffer.Take(2).ToUShort();

        public int GetInt32()
            => Buffer.Take(4).ToInt();

        public uint GetUInt32()
            => Buffer.Take(4).ToUInt();

        public long GetInt64()
            => Compression.DecompressLong(Buffer);

        public ulong GetUInt64()
            => Compression.DecompressULong(Buffer);

        public float GetFloat()
            => Buffer.Take(8).ToFloating();

        public double GetDouble()
            => Buffer.Take(8).ToDouble();

        public bool GetBool()
            => Buffer.Take(1)[0] == 1;

        public char GetChar()
            => (char)Buffer.Take(1)[0];

        public string GetString()
        {
            var stringByte = GetByte();

            if (stringByte == 0)
                return null;

            if (stringByte == 1)
                return "";

            var stringSize = GetInt32();
            var stringBytes = GetBytes(stringSize);
            var stringArray = new char[stringSize];

            for (int i = 0; i < stringSize; i++)
                stringArray[i] = (char)stringBytes[i];

            return new string(stringArray);
        }

        public DateTime GetDateTime()
            => DateTime.FromBinary(GetInt64());

        public DateTimeOffset GetDateTimeOffset()
            => DateTimeOffset.FromUnixTimeSeconds(GetInt64());

        public TimeSpan GetTimeSpan()
            => TimeSpan.FromTicks(GetInt64());

        public IPAddress GetIPAddress()
            => new IPAddress(GetBytes());

        public IPEndPoint GetIPEndPoint()
            => new IPEndPoint(GetIPAddress(), (int)GetUInt16());

        public new Type GetType()
        {
            var name = GetString();

            if (!TypeCache.TryRetrieve(name, out var type))
                throw new TypeLoadException($"Failed to find a type with a name matching '{name}'");

            return type;
        }

        public MemberInfo GetMember()
        {
            var type = GetType();
            var token = GetInt32();

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (member.MetadataToken == token)
                    return member;
            }

            throw new MissingMemberException($"Failed to find a member with a token matching '{token}'");
        }

        public object GetObject()
        {
            var objectByte = GetByte();

            if (objectByte == 0)
                return null;

            var objectType = GetType();

            if (objectByte == 1)
            {
                var objectValue = objectType.Construct<IDeserializableObject>();
                objectValue.Deserialize(this);
                return objectValue;
            }

            if (objectByte == 2 && Deserialization.TryGetDeserializer(objectType, out var deserializer))
                return deserializer(this);

            throw new Exception($"No deserializers were assigned for type '{objectType.FullName}' (deserialization code: {objectByte})");
        }

        public T Get<T>()
        {
            var objectByte = GetByte();

            if (objectByte == 0)
                return default;

            if (objectByte == 1)
            {
                var objectValue = typeof(T).Construct<T>();
                (objectValue as IDeserializableObject).Deserialize(this);
                return objectValue;
            }

            if (objectByte == 2 && Deserialization.TryGetDeserializer(typeof(T), out var deserializer))
                return (T)deserializer(this);

            if (objectByte == 3)
                return MessagePackSerializer.Deserialize<T>(GetBytes(), MessagePack.Resolvers.ContractlessStandardResolver.Options);

            throw new ArgumentOutOfRangeException(nameof(objectByte));
        }

        public T GetDeserializable<T>() where T : IDeserializableObject
        {
            var objectByte = GetByte();

            if (objectByte == 0)
                return default;

            var objectValue = typeof(T).Construct<T>();

            objectValue.Deserialize(this);

            return objectValue;
        }

        public T GetEnum<T>() where T : struct, Enum
            => (T)Deserialization.GetEnum(this);

        public T? GetNullable<T>() where T : struct
        {
            if (GetBool())
                return null;

            return (T)GetObject();
        }

        public List<T> GetList<T>()
        {
            var size = GetInt32();
            var list = new List<T>(size);

            for (int i = 0; i < size; i++)
                list.Add((T)GetObject());

            return list;
        }

        public HashSet<T> GetHashSet<T>()
        {
            var size = GetInt32();
            var set = new HashSet<T>(size);

            for (int i = 0; i < size; i++)
                set.Add((T)GetObject());

            return set;
        }

        public Dictionary<TKey, TValue> GetDictionary<TKey, TValue>()
        {
            var size = GetInt32();
            var dict = new Dictionary<TKey, TValue>();

            for (int i = 0; i < size; i++)
                dict[(TKey)GetObject()] = (TValue)GetObject();

            return dict;
        }

        public static Deserializer GetDeserializer(byte[] data)
            => DeserializerPool.Shared.Rent(data);

        public static void Deserialize(byte[] data, Action<Deserializer> action)
        {
            var deserializer = GetDeserializer(data);
            action(deserializer);
            DeserializerPool.Shared.Return(deserializer);
        }
    }
}