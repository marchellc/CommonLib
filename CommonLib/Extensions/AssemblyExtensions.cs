using CommonLib.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonLib.Extensions
{
    public static class AssemblyExtensions
    {
        private static readonly Dictionary<Assembly, List<Type>> _types = new Dictionary<Assembly, List<Type>>();
        private static readonly Dictionary<Assembly, List<MethodInfo>> _methods = new Dictionary<Assembly, List<MethodInfo>>();

        public static Func<Assembly, byte[]> RawBytesMethod { get; }
        public static Type RuntimeAssemblyType { get; }

        static AssemblyExtensions()
        {
            RuntimeAssemblyType = Type.GetType("System.Reflection.RuntimeAssembly");

            if (RuntimeAssemblyType is null)
            {
                LogOutput.CommonLib.Warn($"Type 'System.Reflection.RuntimeAssembly' is not present in this runtime!");
                return;
            }

            var runtimeAssemblyMethod = RuntimeAssemblyType.GetAllMethods().FirstOrDefault(m => m.Name == "GetRawBytes" && m.ReturnType == typeof(byte[]) && m.Parameters().Length == 0);

            if (runtimeAssemblyMethod is null)
            {
                LogOutput.CommonLib.Warn($"RuntimeAssembly.GetRawBytes method does not exist in this runtime!");
                return;
            }

            RawBytesMethod = assembly => (byte[])runtimeAssemblyMethod.Invoke(assembly, null);
        }

        public static IEnumerable<Type> Types<T>(this Assembly assembly)
        {
            if (!_types.TryGetValue(assembly, out var types))
                types = _types[assembly] = assembly.GetTypes().ToList();

            return types.Where(type => type.InheritsType<T>());
        }

        public static IEnumerable<Type> TypesWithAttribute<T>(this Assembly assembly) where T : Attribute
        {
            if (!_types.TryGetValue(assembly, out var types))
                types = _types[assembly] = assembly.GetTypes().ToList();

            return types.Where(type => type.HasAttribute<T>());
        }

        public static byte[] GetRawBytes(this Assembly assembly)
        {
            if (RawBytesMethod is null)
                throw new InvalidOperationException($"GetRawBytes method is not supported in this runtime!");

            return RawBytesMethod(assembly);
        }

        public static MethodInfo[] Methods(this Assembly assembly)
        {
            if (_methods.TryGetValue(assembly, out var methods))
                return methods.ToArray();

            methods = new List<MethodInfo>();

            foreach (var type in assembly.GetTypes())
                methods.AddRange(type.GetAllMethods());

            return (_methods[assembly] = methods).ToArray();
        }

        public static IEnumerable<MethodInfo> MethodsWithAttribute<T>(this Assembly assembly) where T : Attribute
        {
            if (_methods.TryGetValue(assembly, out var methods))
                return methods.Where(m => m.HasAttribute<T>());

            methods = new List<MethodInfo>();

            foreach (var type in assembly.GetTypes())
                methods.AddRange(type.GetAllMethods());

            return (_methods[assembly] = methods).Where(m => m.HasAttribute<T>());
        }

        public static void InvokeStaticMethods(this Assembly assembly, Func<MethodInfo, bool> predicate, params object[] args)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetAllMethods())
                {
                    try
                    {
                        if (!predicate(method))
                            continue;

                        method.Invoke(null, args);
                    }
                    catch { }
                }
            }
        }
    }
}