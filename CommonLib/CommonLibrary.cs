using CommonLib.Utilities.Console;

using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace CommonLib
{
    public class CommonLibrary
    {
        private static volatile string cachedAppName;
        private static volatile ConcurrentStack<Type> loadedTypes = new ConcurrentStack<Type>();
        
        public static volatile Random Random = new Random();

        public static Assembly Assembly { get; private set; }
        public static Version Version { get; private set; }

        public static void Initialize(IEnumerable<string> arguments)
        {
            try
            {
                Assembly = Assembly.GetExecutingAssembly();
                Version = Assembly.GetName().Version;

                ConsoleArgs.Parse(arguments?.ToArray() ?? Array.Empty<string>());
                CommonLog.IsDebugEnabled = ConsoleArgs.HasSwitch("commonLibDebug");

                if (ConsoleArgs.HasSwitch("commonLibInvariantCulture"))
                {
                    try
                    {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                    }
                    catch { }
                }

                if (ConsoleArgs.HasSwitch("commonLibCommands"))
                    ConsoleCommands.Enable();

                if (AppDomain.CurrentDomain != null)
                    AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
            }
            catch (Exception ex)
            {
                CommonLog.Raw(ex);
            }
        }

        public static void Unload()
        {
            if (AppDomain.CurrentDomain != null)
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoaded;
            
            Assembly = null;
            
            cachedAppName = null;
            
            loadedTypes?.Clear();
            loadedTypes = null;
        }

        public static string GetAppName()
        {
            try
            {
                if (cachedAppName != null)
                    return cachedAppName;

                var entryAssembly = Assembly.GetEntryAssembly();

                if (entryAssembly != null)
                {
                    var entryName = entryAssembly.GetName();

                    if (entryName != null && !string.IsNullOrWhiteSpace(entryName.Name))
                        return cachedAppName = entryName.Name;
                }

                using (var proc = Process.GetCurrentProcess())
                    return cachedAppName = System.IO.Path.GetFileNameWithoutExtension(proc.ProcessName);
            }
            catch { return cachedAppName = "Default App"; }
        }

        public static IEnumerable<Type> SafeQueryTypes()
        {
            if (loadedTypes != null)
                return loadedTypes;

            var assemblies = new List<Assembly>();

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        assemblies.Add(asm);
                    }
                    catch { }
                }
            }
            catch { }

            try
            {
                var curAsm = Assembly.GetExecutingAssembly();

                if (!assemblies.Contains(curAsm))
                    assemblies.Add(curAsm);
            }
            catch { }

            try
            {
                var callAsm = Assembly.GetCallingAssembly();

                if (callAsm != null)
                    assemblies.Add(callAsm);
            }
            catch { }

            loadedTypes = new ConcurrentStack<Type>();
            
            try
            {
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            try
                            {
                                loadedTypes.Push(type);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            return loadedTypes;
        }

        private static void OnAssemblyLoaded(object _, AssemblyLoadEventArgs ev)
        {
            if (ev.LoadedAssembly is null)
                return;

            loadedTypes ??= new ConcurrentStack<Type>();
            
            try
            {
                foreach (var type in ev.LoadedAssembly.GetTypes())
                {
                    try
                    {
                        if (loadedTypes.Contains(type))
                            continue;
                        
                        loadedTypes.Push(type);
                    }
                    catch
                    {
                        
                    }
                }
            }
            catch
            {
                
            }
        }
    }
}
