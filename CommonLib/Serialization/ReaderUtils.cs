using CommonLib.Extensions;
using CommonLib.Logging;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Generic;

namespace CommonLib.Serialization
{
    public static class ReaderUtils
    {
        public static LogOutput Debug = new LogOutput("ReaderUtils").Setup();

        public static void Read(this Stream stream, Action<BinaryReader> action)
        {
            using (var reader = new BinaryReader(stream, WriterUtils.Encoding, true))
                action(reader);
        }

        public static void Read(this byte[] bytes, Action<BinaryReader> action)
        {
            using (var input = new MemoryStream(bytes))
            using (var reader = new BinaryReader(input, WriterUtils.Encoding, true))
                action(reader);
        }

        public static byte[] ReadBytes(this BinaryReader reader)
            => reader.ReadBytes(reader.ReadInt32());

        public static DateTime ReadDate(this BinaryReader reader)
            => DateTime.FromBinary(reader.DecompressLong());

        public static DateTimeOffset ReadOffset(this BinaryReader reader)
            => DateTimeOffset.FromUnixTimeMilliseconds(reader.DecompressLong());

        public static TimeSpan ReadSpan(this BinaryReader reader)
            => TimeSpan.FromTicks(reader.DecompressLong());

        public static IPAddress ReadIpAddress(this BinaryReader reader)
            => new IPAddress(reader.ReadBytes());

        public static IPEndPoint ReadIpEndPoint(this BinaryReader reader)
            => new IPEndPoint(reader.ReadIpAddress(), reader.ReadUInt16());

        public static Type ReadType(this BinaryReader reader)
        {
            var num = reader.ReadByte();

            if (num == 0)
            {
                var code = reader.ReadUInt16();

                return Serialization.TypeCodes.First(p => p.Value == code).Key;
            }
            else
            {
                var name = reader.ReadString();

                return Type.GetType(name, true);
            }
        }

        public static T ReadEnum<T>(this BinaryReader reader) where T : struct, Enum
            => (T)Deserialization.ReadEnum(reader);

        public static object ReadObject(this BinaryReader reader)
        {
            if (reader.ReadBoolean())
                return null;

            var type = reader.ReadType();

            if (type.InheritsType<IDeserializableObject>())
            {
                var obj = type.Construct<IDeserializableObject>();

                obj.Read(reader);
                return obj;
            }

            if (Deserialization.TryGetDeserializer(type, out var deserializer))
                return deserializer(reader);
            else
                throw new InvalidOperationException($"No deserializers for type {type.FullName}");
        }

        public static T ReadDeserializable<T>(this BinaryReader reader) where T : IDeserializableObject
        {
            if (reader.ReadBoolean())
                return default;

            var obj = typeof(T).Construct<T>();

            obj.Read(reader);
            return obj;
        }

        public static T ReadDeserializable<T>(this BinaryReader reader, T obj) where T : IDeserializableObject
        {
            if (reader.ReadBoolean())
                return obj;

            obj.Read(reader);
            return obj;
        }

        public static T ReadAnonymous<T>(this BinaryReader reader)
        {
            var obj = reader.ReadObject();

            if (obj is null)
                return default;

            return (T)obj;
        }

        public static T[] ReadArray<T>(this BinaryReader reader)
        {
            var size = reader.ReadInt32();
            var array = new T[size];

            for (int i = 0; i < size; i++)
                array[i] = reader.ReadAnonymous<T>();

            return array;
        }

        public static List<T> ReadList<T>(this BinaryReader reader)
        {
            var size = reader.ReadInt32();
            var list = new List<T>(size);

            for (int i = 0; i < size; i++)
                list.Add(reader.ReadAnonymous<T>());

            return list;
        }

        public static HashSet<T> ReadHashSet<T>(this BinaryReader reader)
        {
            var size = reader.ReadInt32();
            var set = new HashSet<T>(size);

            for (int i = 0; i < size; i++)
                set.Add(reader.ReadAnonymous<T>());

            return set;
        }

        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(this BinaryReader reader)
        {
            var size = reader.ReadInt32();
            var dict = new Dictionary<TKey, TValue>(size);

            for (int i = 0; i < size; i++)
                dict.Add(reader.ReadAnonymous<TKey>(), reader.ReadAnonymous<TValue>());

            return dict;
        }
    }
}
