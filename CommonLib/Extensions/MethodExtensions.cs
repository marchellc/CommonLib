using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace CommonLib.Extensions
{
    public static class MethodExtensions
    {
        private static readonly BindingFlags _flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, MethodInfo[]> _methods = new Dictionary<Type, MethodInfo[]>();
        private static readonly Dictionary<MethodBase, ParameterInfo[]> _params = new Dictionary<MethodBase, ParameterInfo[]>();

        public static readonly OpCode[] OneByteCodes;
        public static readonly OpCode[] TwoByteCodes;

        static MethodExtensions()
        {
            OneByteCodes = new OpCode[225];
            TwoByteCodes = new OpCode[31];

            foreach (var field in typeof(OpCodes).GetAllFields())
            {
                if (!field.IsStatic || field.FieldType != typeof(OpCode))
                    continue;

                var opCode = field.Get<OpCode>();

                if (opCode.OpCodeType is OpCodeType.Nternal)
                    continue;

                if (opCode.Size == 1)
                    OneByteCodes[opCode.Value] = opCode;
                else
                    TwoByteCodes[opCode.Value & byte.MaxValue] = opCode;
            }
        }

        public static MethodInfo ToGeneric(this MethodInfo method, params Type[] args)
            => method.MakeGenericMethod(args);

        public static MethodInfo ToGeneric<T>(this MethodInfo method)
            => method.ToGeneric(typeof(T));

        public static MethodInfo[] GetAllMethods(this Type type)
        {
            if (_methods.TryGetValue(type, out var methods))
                return methods;

            return _methods[type] = type.GetMembers(_flags).Where(m => m is MethodInfo).Select(m => (MethodInfo)m).ToArray();
        }

        public static ParameterInfo[] Parameters(this MethodBase method)
        {
            if (_params.TryGetValue(method, out var parameters))
                return parameters;

            return _params[method] = method.GetParameters();
        }

        public static MethodInfo Method(this Type type, string name, bool ignoreCase = false)
            => GetAllMethods(type).FirstOrDefault(m => ignoreCase ? m.Name.ToLower() == name.ToLower() : m.Name == name);

        public static MethodInfo Method(this Type type, string name, bool ignoreCase, params Type[] typeArguments)
            => GetAllMethods(type).FirstOrDefault(m => (ignoreCase ? m.Name.ToLower() == name.ToLower() : m.Name == name) && m.Parameters().Select(p => p.ParameterType).IsMatch(typeArguments));

        public static MethodInfo[] MethodsWithAttribute<T>(this Type type) where T : Attribute
            => GetAllMethods(type).Where(m => m.IsDefined(typeof(T), true)).ToArray();

        public static bool TryCreateDelegate<TDelegate>(this MethodInfo method, object target, out TDelegate del) where TDelegate : Delegate
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic && !method.DeclaringType.IsValidInstance(target))
                throw new ArgumentNullException(nameof(target));

            try
            {
                del = Delegate.CreateDelegate(typeof(TDelegate), target, method) as TDelegate;
                return del != null;
            }
            catch (Exception ex)
            {
                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate(this MethodInfo method, Type delegateType, out Delegate del)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic)
                throw new ArgumentException($"Use the other overload for non-static methods!");

            try
            {
                del = Delegate.CreateDelegate(delegateType, method);
                return del != null;
            }
            catch (Exception ex)
            {
                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate(this MethodInfo method, object target, Type delegateType, out Delegate del)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic && !method.DeclaringType.IsValidInstance(target))
                throw new ArgumentNullException(nameof(target));

            try
            {
                del = Delegate.CreateDelegate(delegateType, target, method);
                return del != null;
            }
            catch (Exception ex)
            {
                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate<TDelegate>(this MethodInfo method, out TDelegate del) where TDelegate : Delegate
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic)
                throw new ArgumentException($"Use the other overload for non-static methods!");

            try
            {
                del = Delegate.CreateDelegate(typeof(TDelegate), method) as TDelegate;
                return del != null;
            }
            catch (Exception ex)
            {
                del = null;
                return false;
            }
        }

        public static object Call(this MethodInfo method, params object[] args)
            => InternalCall(method, null, args);

        public static object Call(this MethodInfo method, object target, params object[] args)
            => InternalCall(method, target, args);

        public static T Call<T>(this MethodInfo method, params object[] args)
            => (T)InternalCall(method, null, args);

        public static T Call<T>(this MethodInfo method, object target, params object[] args)
            => (T)InternalCall(method, target, args);

        public static object TryCall(this MethodInfo method, params object[] args)
        {
            try
            {
                return InternalCall(method, null, args);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static object TryCall(this MethodInfo method, object target, params object[] args)
        {
            try
            {
                return InternalCall(method, target, args);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static T TryCall<T>(this MethodInfo method, params object[] args)
        {
            try
            {
                return (T)InternalCall(method, null, args);
            }
            catch (Exception ex)
            {
                return default;
            }
        }

        public static T TryCall<T>(this MethodInfo method, object target, params object[] args)
        {
            try
            {
                return (T)InternalCall(method, target, args);
            }
            catch (Exception ex)
            {
                return default;
            }
        }

        private static object InternalCall(MethodInfo method, object target, object[] args)
        {
            return method.Invoke(target, args);
        }
    }
}