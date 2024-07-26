using CommonLib.Extensions;
using CommonLib.Logging;

using MessagePack;

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace CommonLib.Serialization
{
    public static class Serialization
    {
        private static volatile Dictionary<Type, Action<object, BinaryWriter>> _cache = new Dictionary<Type, Action<object, BinaryWriter>>();

        private static volatile MethodInfo _cachedEnumerable = typeof(WriterUtils).Method("WriteItems");
        private static volatile MethodInfo _cachedDictionary = typeof(WriterUtils).Method("WriteDictionary");
        private static volatile MethodInfo _cachedNullable = typeof(WriterUtils).Method("WriteNullable");

        private static volatile Action<object, BinaryWriter> _cachedDefault = DefaultWriter;
        private static volatile Action<object, BinaryWriter> _cachedEnum = DefaultEnum;

        private static volatile bool _serializersLoaded;

        public static volatile Dictionary<Type, ushort> TypeCodes = new Dictionary<Type, ushort>();
        public static volatile LogOutput Log = new LogOutput("Serialization").Setup();

        public static bool TryGetSerializer(Type type, bool allowDefault, out Action<object, BinaryWriter> serializer)
        {
            if (!_serializersLoaded)
                LoadSerializers();

            if (_cache.TryGetValue(type, out serializer))
                return true;

            if (type.IsEnum)
            {
                serializer = _cachedEnum;
                return true;
            }
            else if (type.IsArray)
            {
                var arrayType = type.GetElementType();
                var arrayMethod = _cachedEnumerable.MakeGenericMethod(arrayType);

                _cache[type] = serializer = (value, serializer) => arrayMethod.Call(serializer, value);
                return true;
            }
            else if (type.GetTypeInfo().IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    var elementType = type.GetFirstGenericType();
                    var elementMethod = _cachedEnumerable.MakeGenericMethod(elementType);

                    _cache[type] = serializer = (value, serializer) => elementMethod.Call(serializer, value);
                    return true;
                }
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var args = type.GetGenericArguments();
                    var keyType = args[0];
                    var elementType = args[1];
                    var elementMethod = _cachedDictionary.MakeGenericMethod(keyType, elementType);

                    _cache[type] = serializer = (value, serializer) => elementMethod.Call(serializer, value);
                    return true;
                }
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                var elementMethod = _cachedNullable.MakeGenericMethod(type);
                _cache[type] = serializer = (value, serializer) => elementMethod.Call(serializer, value);
                return true;
            }

            if (!allowDefault)
            {
                serializer = null;
                return false;
            }

            serializer = _cachedDefault;
            return true;
        }

        public static void RegisterSerializer(Type type, Action<object, BinaryWriter> serializer)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (serializer is null)
                throw new ArgumentNullException(nameof(serializer));

            _cache[type] = serializer;
        }

        public static void RegisterType(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            TypeCodes[type] = type.FullName.GetShortCode();

            Log.Info($"Registered type code: {type.FullName} ({type.FullName.GetShortCode()})");
        }

        public static void RegisterTypes(params Type[] types)
            => types.ForEach(RegisterType);

        public static void LoadSerializers()
        {
            _cache.Clear();
            _serializersLoaded = false;

            Log.Info("Loading serializers ..");

            foreach (var method in typeof(WriterUtils).GetAllMethods())
            {
                if (!method.Name.StartsWith("Write"))
                    continue;

                if (!method.IsStatic || method.IsGenericMethod || method.IsGenericMethodDefinition)
                    continue;

                var parameters = method.Parameters();

                if (parameters.Length != 2 || parameters[0].ParameterType != typeof(BinaryWriter))
                    continue;

                var serializedType = parameters[1].ParameterType;
                var serializerMethod = new Action<object, BinaryWriter>((value, writer) => method.Call(null, writer, value));

                _cache[serializedType] = serializerMethod;

                RegisterType(serializedType);
            }

            foreach (var method in typeof(BinaryWriter).GetAllMethods())
            {
                if (method.Name != "Write")
                    continue;

                if (method.IsStatic || method.IsGenericMethod || method.IsGenericMethodDefinition)
                    continue;

                var parameters = method.Parameters();

                if (parameters.Length != 1)
                    continue;

                var serializedType = parameters[0].ParameterType;

                if (_cache.ContainsKey(serializedType))
                    continue;

                var serializerMethod = new Action<object, BinaryWriter>((value, writer) => method.Call(writer, value));

                _cache[serializedType] = serializerMethod;

                RegisterType(serializedType);
            }

            Log.Info($"Loaded {_cache.Count} serializers.");

            _serializersLoaded = true;
        }

        internal static void DefaultEnum(object value, BinaryWriter writer)
        {
            var enumType = value.GetType();
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var enumValue = Convert.ChangeType(value, underlyingType);

            if (!TryGetSerializer(underlyingType, true, out var enumSerializer))
                throw new InvalidOperationException($"No writers are present for enum type {underlyingType.FullName}");

            writer.Write(enumType);
            enumSerializer(value, writer);
        }

        private static void DefaultWriter(object value, BinaryWriter writer)
        {
            var type = value.GetType();
            var bytes = MessagePackSerializer.Serialize(type, value, MessagePack.Resolvers.ContractlessStandardResolver.Options);

            writer.Write(type);
            writer.WriteBytes(bytes);
        }
    }
}