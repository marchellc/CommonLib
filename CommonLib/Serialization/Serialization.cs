using CommonLib.Extensions;

using MessagePack;

using System;
using System.Reflection;
using System.Collections.Generic;

namespace CommonLib.Serialization
{
    public static class Serialization
    {
        private static readonly Dictionary<Type, Action<object, Serializer>> _cache = new Dictionary<Type, Action<object, Serializer>>();

        private static readonly MethodInfo _cachedEnumerable = typeof(Serializer).Method("PutItems");
        private static readonly MethodInfo _cachedDictionary = typeof(Serializer).Method("PutPairs");
        private static readonly MethodInfo _cachedNullable = typeof(Serializer).Method("PutNullable");

        private static readonly Action<object, Serializer> _cachedDefault = DefaultSerializer;
        private static readonly Action<object, Serializer> _cachedEnum = DefaultEnum;

        private static bool _serializersLoaded;

        public static bool TryGetSerializer(Type type, bool allowDefault, out Action<object, Serializer> serializer)
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

        private static void LoadSerializers()
        {
            _cache.Clear();
            _serializersLoaded = false;

            foreach (var method in typeof(Serializer).GetAllMethods())
            {
                if (!method.Name.StartsWith("Put"))
                    continue;

                if (method.IsStatic || method.IsGenericMethod || method.IsGenericMethodDefinition)
                    continue;

                var parameters = method.Parameters();

                if (parameters.Length != 1)
                    continue;

                var serializedType = parameters[0].ParameterType;
                var serializerMethod = new Action<object, Serializer>((value, serializer) => method.Call(serializer, value));

                _cache[serializedType] = serializerMethod;
            }

            foreach (var type in CommonLibrary.SafeQueryTypes())
            {
                if (type == typeof(Serializer) || type == typeof(Serialization))
                    continue;

                foreach (var method in type.GetAllMethods())
                {
                    if (!method.IsStatic || method.IsGenericMethod || method.IsGenericMethodDefinition)
                        continue;

                    var parameters = method.Parameters();

                    if (parameters.Length != 2 || parameters[0].ParameterType != typeof(Serializer))
                        continue;

                    var serializedType = parameters[1].ParameterType;
                    var serializerMethod = new Action<object, Serializer>((value, serializer) => method.Call(null, serializer, value));

                    _cache[serializedType] = serializerMethod;
                }
            }

            _serializersLoaded = true;
        }

        internal static void DefaultEnum(object value, Serializer serializer)
        {
            var enumType = value.GetType();
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var enumValue = Convert.ChangeType(value, underlyingType);

            if (!TryGetSerializer(underlyingType, true, out var enumSerializer))
                throw new InvalidOperationException($"No writers are present for enum type {underlyingType.FullName}");

            serializer.Put(enumType);
            enumSerializer(value, serializer);
        }

        private static void DefaultSerializer(object value, Serializer serializer)
        {
            var type = value.GetType();
            var bytes = MessagePackSerializer.Serialize(type, value, MessagePack.Resolvers.ContractlessStandardResolver.Options);

            serializer.Put(type);
            serializer.Put(bytes);
        }
    }
}