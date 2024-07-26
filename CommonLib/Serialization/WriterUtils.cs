using CommonLib.Logging;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace CommonLib.Serialization
{
    public static class WriterUtils
    {
        public static LogOutput Debug = new LogOutput("Serializer").Setup();
        public static Encoding Encoding = Encoding.ASCII;

        public static void Write(this Stream stream, Action<BinaryWriter> action)
        {
            using (var writer = new BinaryWriter(stream, Encoding, true))
                action(writer);
        }

        public static byte[] Write(Action<BinaryWriter> action)
        {
            using (var output = new MemoryStream())
            using (var writer = new BinaryWriter(output, Encoding, true))
            {
                action(writer);
                return output.ToArray();
            }
        }

        public static void WriteBytes(this BinaryWriter writer, byte[] bytes)
        {
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public static void WriteString(this BinaryWriter writer, string str)
        {
            str ??= string.Empty;

            writer.Write(str);
        }

        public static void Write(this BinaryWriter writer, DateTime time)
            => writer.CompressLong(time.ToBinary());

        public static void Write(this BinaryWriter writer, DateTimeOffset offset)
            => writer.CompressLong(offset.ToUnixTimeMilliseconds());

        public static void Write(this BinaryWriter writer, TimeSpan span)
            => writer.CompressLong(span.Ticks);

        public static void Write(this BinaryWriter writer, IPAddress address)
            => writer.WriteBytes(address.GetAddressBytes());

        public static void Write(this BinaryWriter writer, IPEndPoint endPoint)
        {
            writer.Write(endPoint.Address);
            writer.Write((ushort)endPoint.Port);
        }

        public static void Write(this BinaryWriter writer, Type type)
        {
            if (Serialization.TypeCodes.TryGetValue(type, out var code))
            {
                writer.Write((byte)0);
                writer.Write(code);
            }
            else
            {
                writer.Write((byte)1);
                writer.Write(type.AssemblyQualifiedName);
            }
        }

        public static void WriteEnum(this BinaryWriter writer, Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var numericValue = Convert.ChangeType(enumValue, underlyingType);

            if (!Serialization.TryGetSerializer(underlyingType, true, out var enumSerializer))
                throw new InvalidOperationException($"No writers are present for enum type {underlyingType.FullName}");

            writer.Write(enumType);
            enumSerializer(numericValue, writer);
        }

        public static void WriteObject(this BinaryWriter writer, object value)
        {
            if (value is null)
            {
                writer.Write(true);
                return;
            }

            writer.Write(false);
            writer.Write(value.GetType());

            if (value is ISerializableObject serializableObject)
                serializableObject.Write(writer);
            else
            {
                if (Serialization.TryGetSerializer(value.GetType(), true, out var serializer))
                    serializer(value, writer);
                else
                    throw new InvalidOperationException($"No serializers are assigned for type {value.GetType().FullName}");
            }
        }

        public static void WriteSerializable(this BinaryWriter writer, ISerializableObject serializableObject)
        {
            if (serializableObject is null)
            {
                writer.Write(true);
                return;
            }

            writer.Write(false);
            serializableObject.Write(writer);
        }

        public static void WriteItems<T>(this BinaryWriter writer, IEnumerable<T> objects)
        {
            writer.Write(objects.Count());

            foreach (var obj in objects)
                writer.WriteObject(obj);
        }

        public static void WriteDictionary<TKey, TValue>(this BinaryWriter writer, IDictionary<TKey, TValue> dictionary)
        {
            writer.Write(dictionary.Count);

            foreach (var pair in dictionary)
            {
                writer.WriteObject(pair.Key);
                writer.WriteObject(pair.Value);
            }
        }
    }
}
