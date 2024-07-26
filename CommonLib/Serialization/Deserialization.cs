using CommonLib.Extensions;
using CommonLib.Logging;

using MessagePack;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CommonLib.Serialization
{
    public static class Deserialization
    {
        private static volatile Dictionary<Type, Func<BinaryReader, object>> _cache = new Dictionary<Type, Func<BinaryReader, object>>();

        private static volatile MethodInfo _cachedList = typeof(ReaderUtils).Method("ReadList");
        private static volatile MethodInfo _cachedSet = typeof(ReaderUtils).Method("ReadHashSet");
        private static volatile MethodInfo _cachedArray = typeof(ReaderUtils).Method("ReadArray");
        private static volatile MethodInfo _cachedDict = typeof(ReaderUtils).Method("ReadDictionary");
        private static volatile MethodInfo _cachedNullable = typeof(ReaderUtils).Method("ReadNullable");

        private static volatile Func<BinaryReader, object> _cachedDefault = ReadDefault;
        private static volatile Func<BinaryReader, object> _cachedEnum = ReadEnum;

        private static volatile bool _deserializersLoaded;

        public static readonly LogOutput Log = new LogOutput("Deserialization").Setup();

        public static bool TryGetDeserializer(Type type, out Func<BinaryReader, object> deserializer)
        {
            if (!_deserializersLoaded)
                LoadDeserializers();

            if (_cache.TryGetValue(type, out deserializer))
                return true;

            if (type.IsEnum)
            {
                deserializer = _cachedEnum;
                return true;
            }
            else if (type.IsArray)
            {
                var arrayType = type.GetElementType();
                var arrayMethod = _cachedArray.MakeGenericMethod(arrayType);

                _cache[type] = deserializer = des => arrayMethod.Call(des);
                return true;
            }
            else if (type.GetTypeInfo().IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = type.GetFirstGenericType();
                    var listMethod = _cachedList.MakeGenericMethod(listType);

                    _cache[type] = deserializer = des => listMethod.Call(des);
                    return true;
                }
                else if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    var setType = type.GetFirstGenericType();
                    var setMethod = _cachedSet.MakeGenericMethod(setType);

                    _cache[type] = deserializer = des => setMethod.Call(des);
                    return true;
                }
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var dictArgs = type.GetGenericArguments();
                    var dictMethod = _cachedDict.MakeGenericMethod(dictArgs);

                    _cache[type] = deserializer = des => dictMethod.Call(des);
                    return true;
                }
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                var underlyingMethod = _cachedNullable.MakeGenericMethod(type);

                _cache[type] = deserializer = des => underlyingMethod.Call(des);
                return true;
            }

            deserializer = _cachedDefault;
            return true;
        }

        public static void RegisterDeserializer(Type type, Func<BinaryReader, object> deserializer)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (deserializer is null)
                throw new ArgumentNullException(nameof(deserializer));

            _cache[type] = deserializer;
        }

        public static void LoadDeserializers()
        {
            _cache.Clear();
            _deserializersLoaded = false;

            Log.Info("Loading deserializers ..");

            foreach (var method in typeof(ReaderUtils).GetAllMethods())
            {
                if (!method.Name.StartsWith("Read") || !method.IsStatic)
                    continue;

                if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                    continue;

                var methodParams = method.Parameters();

                if (methodParams.Length != 1 || methodParams[0].ParameterType != typeof(BinaryReader))
                    continue;

                var deserializedType = method.ReturnType;
                var deserializerMethod = new Func<BinaryReader, object>(deserializer => method.Call(null, deserializer));

                _cache[deserializedType] = deserializerMethod;
            }

            foreach (var method in typeof(BinaryReader).GetAllMethods())
            {
                if (!method.Name.StartsWith("Read") || method.IsStatic)
                    continue;

                if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                    continue;

                var methodParams = method.Parameters();

                if (methodParams.Length > 0)
                    continue;

                var deserializedType = method.ReturnType;

                if (_cache.ContainsKey(deserializedType))
                    continue;

                var deserializerMethod = new Func<BinaryReader, object>(deserializer => method.Call(deserializer));

                _cache[deserializedType] = deserializerMethod;
            }

            Log.Info($"Loaded {_cache.Count} deserializers.");

            _deserializersLoaded = true;
        }

        internal static object ReadEnum(BinaryReader reader)
        {
            var enumType = reader.GetType();
            var enumNumericalType = Enum.GetUnderlyingType(enumType);

            if (!TryGetDeserializer(enumNumericalType, out var enumDeserializer))
                throw new InvalidOperationException($"Missing numerical deserializer for enum '{enumType.FullName}' ({enumNumericalType.FullName})");

            var enumValue = enumDeserializer(reader);
            return Enum.ToObject(enumType, enumValue);
        }

        private static object ReadDefault(BinaryReader reader)
        {
            var type = reader.ReadType();
            var bytes = reader.ReadBytes();

            return MessagePackSerializer.Deserialize(type, bytes, MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }
    }
}