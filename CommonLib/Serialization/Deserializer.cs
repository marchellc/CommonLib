using CommonLib.Extensions;
using CommonLib.Utilities;
using CommonLib.Serialization.Buffers;
using CommonLib.Serialization.Pooling;

using System;
using System.Net;
using System.Text;
using System.Linq;
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
            => Buffer.Take(8).ToLong();

        public ulong GetUInt64()
            => Buffer.Take(8).ToULong();

        public float GetFloat()
            => Buffer.Take(8).ToFloating();

        public double GetDouble()
            => Buffer.Take(8).ToDouble();

        public bool GetBool()
            => Buffer.Take(1)[0] == 1;

        public char GetChar()
            => (char)Buffer.Take(1)[0];

        public string GetString()
            => Encoding.UTF32.GetString(GetBytes());

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
            var typeId = GetUInt16();
            var typeCode = GetUInt16();

            if (typeId == 0)
                return Serialization.TypeCodes.First(p => p.Value == typeCode).Key;
            else
            {
                var typeName = GetString();
                var type = Type.GetType(typeName);

                if (type is null)
                    throw new TypeLoadException();

                Serialization.TypeCodes[type] = typeCode;

                return type;
            }
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
            var objectId = GetUInt16();

            if (objectId == 0)
                return null;

            var objectType = GetType();

            if (objectType.InheritsType<IDeserializableObject>())
            {
                var objectValue = objectType.Construct<IDeserializableObject>();
                objectValue.Deserialize(this);
                return objectValue;
            }

            if (Deserialization.TryGetDeserializer(objectType, out var deserializer))
                return deserializer(this);

            return MessagePackSerializer.Deserialize(objectType, GetBytes(), MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        public T Get<T>()
        {
            var objectId = GetUInt16();

            if (objectId == 0)
                return default;

            if (typeof(T).InheritsType<IDeserializableObject>())
            {
                var objectValue = typeof(T).Construct<T>();
                (objectValue as IDeserializableObject).Deserialize(this);
                return objectValue;
            }

            if (Deserialization.TryGetDeserializer(typeof(T), out var deserializer))
                return (T)deserializer(this);

            return MessagePackSerializer.Deserialize<T>(GetBytes(), MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        public T GetDeserializable<T>() where T : IDeserializableObject
        {
            var objectId = GetUInt16();

            if (objectId == 0)
                return default;

            var objectValue = typeof(T).Construct<T>();
            objectValue.Deserialize(this);
            return objectValue;
        }

        public T GetAnonymous<T>()
        {
            var obj = GetObject();

            if (obj is null)
                return default;

            return (T)obj;
        }

        public T GetEnum<T>() where T : struct, Enum
            => (T)Deserialization.GetEnum(this);

        public T? GetNullable<T>() where T : struct
        {
            if (GetBool())
                return null;

            return GetAnonymous<T>();
        }

        public List<T> GetList<T>()
        {
            var size = GetInt32();
            var list = new List<T>(size);

            for (int i = 0; i < size; i++)
                list.Add(GetAnonymous<T>());

            return list;
        }

        public HashSet<T> GetHashSet<T>()
        {
            var size = GetInt32();
            var set = new HashSet<T>(size);

            for (int i = 0; i < size; i++)
                set.Add(GetAnonymous<T>());

            return set;
        }

        public T[] GetArray<T>()
        {
            var size = GetInt32();
            var array = new T[size];

            for (int i = 0; i < size; i++)
                array[i] = GetAnonymous<T>();

            return array;
        }

        public Dictionary<TKey, TValue> GetDictionary<TKey, TValue>()
        {
            var size = GetInt32();
            var dict = new Dictionary<TKey, TValue>();

            for (int i = 0; i < size; i++)
                dict[GetAnonymous<TKey>()] = GetAnonymous<TValue>();

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