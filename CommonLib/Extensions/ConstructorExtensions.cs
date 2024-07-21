using CommonLib.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonLib.Extensions
{
    public static class ConstructorExtensions
    {
        private static readonly Dictionary<Type, ConstructorInfo[]> _constructors = new Dictionary<Type, ConstructorInfo[]>();
        private static readonly BindingFlags _flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static readonly LogOutput Log = new LogOutput("Constructor Extensions").Setup();

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
        {
            ConstructorInfo constructor = null;

            if (parameters.Length == 0)
                constructor = GetEmptyConstructor(type);
            else
                constructor = GetAllConstructors(type).FirstOrDefault(c => c.Parameters().Select(p => p.ParameterType).IsMatch(parameters.Select(pr => pr.GetType())));

            if (constructor is null)
                throw new InvalidOperationException($"There are no constructors available for type '{type.FullName}' with this declaration.");

            return constructor.Invoke(parameters);
        }

        public static T Construct<T>(this Type type, params object[] parameters)
            => (T)type.Construct(parameters);

        public static T TryConstruct<T>(params object[] parameters)
        {
            try
            {
                var value = typeof(T).Construct(parameters);

                if (value is null || value is not T t)
                    return default;

                return t;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to construct type '{typeof(T).ToName()}' due to an exception:\n{ex}");
                return default;
            }
        }
    }
}
