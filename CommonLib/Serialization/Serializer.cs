using CommonLib.Serialization.Buffers;
using CommonLib.Serialization.Pooling;

using System;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using MessagePack;

namespace CommonLib.Serialization
{
    public class Serializer
    {
        public SerializerBuffer Buffer { get; }

        public Serializer()
            => Buffer = new SerializerBuffer();

        public void Put(byte value)
            => Buffer.Write(value);

        public void Put(bool value)
            => Buffer.Write(value ? (byte)1 : (byte)0);

        public void Put(short value)
        {
            Buffer.Write((byte)value);
            Buffer.Write((byte)(value >> 8));
        }

        public void Put(ushort value)
        {
            Buffer.Write((byte)value);
            Buffer.Write((byte)(value >> 8));
        }

        public void Put(int value)
        {
            Buffer.Write((byte)value);
            Buffer.Write((byte)(value >> 8));
            Buffer.Write((byte)(value >> 16));
            Buffer.Write((byte)(value >> 24));
        }

        public void Put(uint value)
        {
            Buffer.Write((byte)value);
            Buffer.Write((byte)(value >> 8));
            Buffer.Write((byte)(value >> 16));
            Buffer.Write((byte)(value >> 24));
        }

        public void Put(long value)
            => Buffer.CompressLong(value);

        public void Put(ulong value)
            => Buffer.CompressULong(value);

        public unsafe void Put(float value)
        {
            var temp = *(uint*)&value;
            Put(temp);
        }

        public unsafe void Put(double value)
        {
            var temp = *(ulong*)&value;
            Put(temp);
        }

        public void PutBytes(IEnumerable<byte> bytes)
        {
            Put(bytes.LongCount());
            Buffer.Write(bytes);
        }

        public void Put(char value)
            => Buffer.Write((byte)value);

        public void Put(string value)
        {
            if (value is null)
            {
                Buffer.Write((byte)0);
                return;
            }

            if (value == "")
            {
                Buffer.Write((byte)1);
                return;
            }

            Buffer.Write((byte)2);

            Put(value.Length);

            for (int i = 0; i < value.Length; i++)
                Put(value[i]);
        }

        public void Put(DateTime value)
            => Put(value.ToBinary());

        public void Put(DateTimeOffset value)
            => Put(value.ToUnixTimeSeconds());

        public void Put(TimeSpan value)
            => Put(value.Ticks);

        public void Put(IPAddress address)
            => Put(address.GetAddressBytes());

        public void Put(IPEndPoint endPoint)
        {
            Put(endPoint.Address);
            Put((ushort)endPoint.Port);
        }

        public void Put(Type type)
            => Put(type.AssemblyQualifiedName);

        public void Put(MemberInfo member)
        {
            Put(member.DeclaringType);
            Put(member.MetadataToken);
        }

        public void Put<T>(T value)
        {
            if (value is null)
            {
                Buffer.Write((byte)0);
                return;
            }

            if (value is ISerializableObject serializableObject)
            {
                Buffer.Write((byte)1);
                serializableObject.Serialize(this);
                return;
            }

            if (Serialization.TryGetSerializer(typeof(T), false, out var serializer))
            {
                Buffer.Write((byte)2);
                serializer(value, this);
                return;
            }

            Buffer.Write((byte)3);
            Put(MessagePackSerializer.Serialize(typeof(T), value, MessagePack.Resolvers.ContractlessStandardResolver.Options));
        }

        public void PutEnum<T>(T enumValue) where T : struct, Enum
            => Serialization.DefaultEnum(enumValue, this);

        public void PutNullable<T>(T? value) where T : struct
        {
            if (!value.HasValue)
            {
                Put(false);
                return;
            }

            Put(true);
            Put(value.Value);
        }

        public void PutItems<T>(IEnumerable<T> values)
        {
            Put(values.Count());

            foreach (var value in values)
                Put(value);
        }

        public void PutPairs<TKey, TValue>(IDictionary<TKey, TValue> values)
        {
            Put(values.Count());

            foreach (var pair in values)
            {
                Put(pair.Key);
                Put(pair.Value);
            }
        }

        public void PutObject(object value)
        {
            if (value is null)
            {
                Buffer.Write((byte)0);
                return;
            }

            var type = value.GetType();

            if (value is ISerializableObject serializableObject)
            {
                Buffer.Write((byte)1);
                Put(type);
                serializableObject.Serialize(this);
                return;
            }

            if (Serialization.TryGetSerializer(type, true, out var serializer))
            {
                Buffer.Write((byte)2);
                serializer(value, this);
                return;
            }

            throw new InvalidOperationException($"No serializers were available for object {type.FullName}");
        }

        public byte[] Return()
        {
            SerializerPool.Shared.Return(this);
            return Buffer.Data;
        }

        public static byte[] Serialize(Action<Serializer> action)
        {
            var serializer = SerializerPool.Shared.Rent();
            action(serializer);
            return serializer.Return();
        }
    }
}