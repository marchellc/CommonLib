using CommonLib.Pooling.Pools;
using CommonLib.Logging.File;
using CommonLib.Logging;
using CommonLib.Utilities;
using CommonLib.Extensions;
using CommonLib.IO;

using System;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace CommonLib
{
    public class CommonLibrary
    {
        private static string cachedAppName;

        public static event Action OnInitialized;
        public static event Action OnUnloaded;

        public static bool IsDebugBuild { get; private set; }
        public static bool IsTraceBuild { get; private set; }
        public static bool IsInitialized { get; private set; }

        public static DateTime InitializedAt { get; private set; }

        public static Assembly Assembly { get; private set; }
        public static Version Version { get; private set; }

        public static Directory Directory { get; private set; }

        public static void Initialize(IEnumerable<string> arguments)
        {
            if (IsInitialized)
                return;

            try
            {
                var initStarted = DateTime.Now;

                IsInitialized = true;

#if DEBUG
                IsDebugBuild = true;
#elif TRACE
                IsTraceBuild = true;
#endif

                Assembly = Assembly.GetExecutingAssembly();
                Version = Assembly.GetName().Version;

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (!System.IO.Directory.Exists($"{appData}/CommonLib Library"))
                    System.IO.Directory.CreateDirectory($"{appData}/CommonLib Library");

                var appName = GetAppName();

                if (!System.IO.Directory.Exists($"{appData}/CommonLib Library/{appName}"))
                    System.IO.Directory.CreateDirectory($"{appData}/CommonLib Library/{appName}");

                Directory = new Directory($"{appData}/CommonLib Library/{appName}");

                LogUtils.Default = IsDebugBuild ? LogUtils.General | LogUtils.Debug : LogUtils.General;
                LogOutput.Init();

                if (!System.IO.Directory.Exists($"{Directory.Path}/Logs"))
                    System.IO.Directory.CreateDirectory($"{Directory.Path}/Logs");

                FileLogger.Init($"{Directory.Path}/{DateTime.Now.Day}_{DateTime.Now.Month} {DateTime.Now.Hour}h {DateTime.Now.Minute}m.txt");

                ConsoleArgs.Parse(arguments?.ToArray() ?? Array.Empty<string>());

                if (IsDebugBuild || ConsoleArgs.HasSwitch("DebugLogs"))
                {
                    LogOutput.CommonLib.Enable(LogLevel.Debug);
                    LogUtils.Default = LogUtils.General | LogUtils.Debug;
                }

                if (ConsoleArgs.HasSwitch("InvariantCulture"))
                {
                    try
                    {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                    }
                    catch { }
                }

                if (ConsoleArgs.HasSwitch("EnableCommands"))
                    ConsoleCommands.Enable();

                MethodExtensions.EnableLogging = ConsoleArgs.HasSwitch("MethodLogger");

                InitializedAt = DateTime.Now;
                OnInitialized.Call();

                LogOutput.CommonLib.Info($"Library initialized (version: {Version}, time: {DateTime.Now.ToString("G")}), took {(InitializedAt - initStarted).TotalSeconds} second(s)!");
            }
            catch (Exception ex)
            {
                LogOutput.Raw(ex, ConsoleColor.Red);
            }
        }

        public static void Unload()
        {
            LogOutput.CommonLib.Info($"Unloading library ..");

            OnUnloaded.Call();
            InitializedAt = default;

            Assembly = null;
            cachedAppName = null;

            LogOutput.CommonLib.Info($"Library unloaded!");

            IsInitialized = false;
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

        public static List<Type> SafeQueryTypes()
        {
            var assemblies = ListPool<Assembly>.Shared.Rent();

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

            var types = new List<Type>();

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
                                types.Add(type);
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return types;
        }
    }
}
