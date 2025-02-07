using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace CommonLib.Extensions
{
    public static class FieldExtensions
    {
        private static readonly Dictionary<Type, FieldInfo[]> _fields = new Dictionary<Type, FieldInfo[]>();
        private static readonly BindingFlags _flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static FieldInfo[] GetAllFields(this Type type)
        {
            if (_fields.TryGetValue(type, out var fields))
                return fields;

            return _fields[type] = type.GetFields(_flags);
        }

        public static FieldInfo Field(this Type type, string name, bool ignoreCase = false)
            => GetAllFields(type).FirstOrDefault(f => ignoreCase ? f.Name.ToLower() == name.ToLower() : f.Name == name);

        public static object Get(this FieldInfo field)
            => field.GetValue(null);

        public static object Get(this FieldInfo field, object target)
            => field.GetValue(target);

        public static T Get<T>(this FieldInfo field)
        {
            var value = Get(field);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static T Get<T>(this FieldInfo field, object target)
        {
            var value = Get(field, target);

            if (value is null || value is not T t)
                return default;

            return t;
        }

        public static void Set(this FieldInfo field, object value)
            => Set(field, null, value);

        public static void Set(this FieldInfo field, object target, object value)
            => field.SetValue(target, value);
    }
}
