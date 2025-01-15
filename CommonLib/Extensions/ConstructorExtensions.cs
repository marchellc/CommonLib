using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace CommonLib.Extensions
{
    public static class ConstructorExtensions
    {
        private static readonly Dictionary<Type, ConstructorInfo[]> _constructors = new Dictionary<Type, ConstructorInfo[]>();
        private static readonly BindingFlags _flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static ConstructorInfo[] GetAllConstructors(this Type type)
        {
            if (_constructors.TryGetValue(type, out var constructors))
                return constructors;

            return _constructors[type] = type.GetConstructors(_flags);
        }

        public static ConstructorInfo GetEmptyConstructor(this Type type)
            => GetAllConstructors(type).FirstOrDefault(c => c.Parameters().Length == 0);

        public static ConstructorInfo GetConstructor(this Type type, params Type[] types)
            => GetAllConstructors(type).FirstOrDefault(c => c.Parameters().Select(p => p.ParameterType).IsMatch(types));

        public static object Construct(this Type type, params object[] parameters)
            => (parameters.Length > 0 ? Activator.CreateInstance(type, true, parameters) : Activator.CreateInstance(type, true));

        public static T Construct<T>(this Type type, params object[] parameters)
            => (T)Construct(type, parameters);

        public static T TryConstruct<T>(params object[] parameters)
        {
            var value = typeof(T).Construct(parameters);

            if (value is null || value is not T t)
                return default;

            return t;
        }
    }
}
