using System;

namespace CommonLib.Extensions
{
    public static class ObjectExtensions
    {
        public static bool IsEqualTo(this object instance, object otherInstance, bool countNull = true)
        {
            if (instance is null && otherInstance is null)
                return countNull;

            if ((instance is null && otherInstance != null) || (instance != null && otherInstance is null))
                return countNull;

            return instance == otherInstance;
        }

        public static T Is<T>(this object value)
        {
            if (value.Is<T>(out var cast))
                return cast;

            return default;
        }

        public static bool Is<T>(this object value, out T castValue)
        {
            if (value is null)
            {
                castValue = default;
                return false;
            }

            if (value is T t)
            {
                castValue = t;
                return true;
            }

            castValue = default;
            return false;
        }

        public static void IfIs<T>(this object value, Action<T> action)
        {
            if (Is<T>(value, out var castValue))
                action.Call(castValue);
        }

        public static bool IsStruct(this object value)
            => value != null && value.GetType().IsValueType;
    }
}