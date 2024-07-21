using CommonLib.Extensions;

using MessagePack;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace CommonLib.Serialization
{
    public static class Deserialization
    {
        private static readonly Dictionary<Type, Func<Deserializer, object>> _cache = new Dictionary<Type, Func<Deserializer, object>>();

        private static readonly MethodInfo _cachedList = typeof(Deserializer).Method("GetList");
        private static readonly MethodInfo _cachedSet = typeof(Deserializer).Method("GetHashSet");
        private static readonly MethodInfo _cachedArray = typeof(Deserializer).Method("GetArray");
        private static readonly MethodInfo _cachedDict = typeof(Deserializer).Method("GetDictionary");
        private static readonly MethodInfo _cachedNullable = typeof(Deserializer).Method("GetNullable");

        private static readonly Func<Deserializer, object> _cachedDefault = GetDefault;
        private static readonly Func<Deserializer, object> _cachedEnum = GetEnum;

        private static bool _deserializersLoaded;

        public static bool TryGetDeserializer(Type type, out Func<Deserializer, object> deserializer)
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

        private static void LoadDeserializers()
        {
            _cache.Clear();
            _deserializersLoaded = false;

            foreach (var method in typeof(Deserializer).GetAllMethods())
            {
                if (!method.Name.StartsWith("Get") || method.IsStatic)
                    continue;

                if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                    continue;

                var methodParams = method.Parameters();

                if (methodParams.Length != 1)
                    continue;

                var deserializedType = method.ReturnType;
                var deserializerMethod = new Func<Deserializer, object>(deserializer => method.Call(deserializer));

                _cache[deserializedType] = deserializerMethod;
            }

            foreach (var type in CommonLibrary.SafeQueryTypes())
            {
                if (type == typeof(Deserialization) || type == typeof(Deserializer))
                    continue;

                foreach (var method in type.GetAllMethods())
                {
                    if (method.IsGenericMethod || method.IsGenericMethodDefinition || !method.IsStatic)
                        continue;

                    var methodParams = method.Parameters();

                    if (methodParams.Length != 1 || methodParams[0].ParameterType != typeof(Deserializer))
                        continue;

                    var deserializedType = method.ReturnType;
                    var deserializerMethod = new Func<Deserializer, object>(deserializer => method.Call(null, deserializer));

                    _cache[deserializedType] = deserializerMethod;
                }
            }

            _deserializersLoaded = true;
        }

        internal static object GetEnum(Deserializer deserializer)
        {
            var enumType = deserializer.GetType();
            var enumNumericalType = Enum.GetUnderlyingType(enumType);

            if (!TryGetDeserializer(enumNumericalType, out var enumDeserializer))
                throw new InvalidOperationException($"Missing numerical deserializer for enum '{enumType.FullName}' ({enumNumericalType.FullName})");

            var enumValue = enumDeserializer(deserializer);
            return Enum.ToObject(enumType, enumValue);
        }

        private static object GetDefault(Deserializer deserializer)
        {
            var type = deserializer.GetType();
            var bytes = deserializer.GetBytes();

            return MessagePackSerializer.Deserialize(type, bytes, MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }
    }
}