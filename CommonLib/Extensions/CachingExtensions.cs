using CommonLib.Caching;

using System;
using System.Linq;

namespace CommonLib.Extensions
{
    public static class CachingExtensions
    {
        public static bool ContainsAny<T>(this ICache<T> cache, Func<T, bool> predicate)
        {
            var array = cache.GetAll().ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                if (predicate.Call(array[i]))
                    return true;
            }

            return false;
        }
    }
}