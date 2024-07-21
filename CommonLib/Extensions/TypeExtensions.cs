﻿using CommonLib.Logging;
using CommonLib.Pooling.Pools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonLib.Extensions
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, ConstructorInfo[]> _constructors = new Dictionary<Type, ConstructorInfo[]>();
        private static readonly Dictionary<Type, Type[]> _implements = new Dictionary<Type, Type[]>();

        private static readonly BindingFlags _flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static readonly LogOutput Log = new LogOutput("Type Extensions").Setup();

        public static Type ToGeneric(this Type type, params Type[] args)
            => type.MakeGenericType(args);

        public static Type ToGeneric<T>(this Type type)
            => type.MakeGenericType(typeof(T));

        public static Type GetFirstGenericType(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            var genericArguments = type.GetGenericArguments();

            if (genericArguments is null || genericArguments.Length <= 0)
                throw new InvalidOperationException($"Attempted to get generic arguments of a type that does not have any.");

            return genericArguments[0];
        }

        public static bool IsStatic(this Type type)
            => type.IsSealed && type.IsAbstract;

        public static bool InheritsType<TType>(this Type type)
            => InheritsType(type, typeof(TType));

        public static bool InheritsType(this Type type, Type inherit)
        {
            if (_implements.TryGetValue(type, out var implements))
                return implements.Contains(inherit);

            var baseType = type.BaseType;
            var cache = ListPool<Type>.Shared.Rent();

            while (baseType != null)
            {
                cache.Add(baseType);
                baseType = baseType.BaseType;
            }

            var interfaces = type.GetInterfaces();

            cache.AddRange(interfaces);

            foreach (var interfaceType in interfaces)
            {
                baseType = interfaceType.BaseType;

                while (baseType != null && baseType.IsInterface)
                {
                    cache.Add(baseType);
                    baseType = baseType.BaseType;
                }
            }

            return (_implements[type] = ListPool<Type>.Shared.ToArrayReturn(cache)).Contains(inherit);
        }

        public static bool IsValidInstance(this Type type, object instance, bool suppliedForStatic = false)
        {
            if (type.IsSealed && type.IsAbstract && instance != null)
                return !suppliedForStatic;

            if (!(type.IsSealed && type.IsAbstract) && instance is null)
                return false;

            if (instance != null && instance.GetType() != type)
                return false;

            return true;
        }
    }
}