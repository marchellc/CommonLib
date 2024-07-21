using CommonLib.Logging;

using System;
using System.Collections.Generic;

namespace CommonLib.Utilities
{
    public static class ConsoleArgs
    {
        private static Dictionary<string, string> keys = new Dictionary<string, string>();
        private static HashSet<string> switches = new HashSet<string>();

        public static void Parse(string[] args)
        {
            if (args is null || args.Length <= 0)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i]))
                    continue;

                if (args[i].StartsWith("--"))
                {
                    if (!args[i].Contains("="))
                    {
                        LogOutput.CommonLib?.Warn($"Failed to parse argument '{args[i]}' at {i}");
                        continue;
                    }

                    var split = args[i].Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length != 2)
                    {
                        LogOutput.CommonLib?.Warn($"Failed to parse argument '{args[i]}' at {i}");
                        continue;
                    }

                    var key = split[0].Replace("--", "").Trim();
                    var value = split[1].Trim();

                    if (keys.ContainsKey(key))
                    {
                        LogOutput.CommonLib?.Warn($"Failed to parse argument '{args[i]}' at {i}: this argument already exists");
                        continue;
                    }

                    keys[key] = value;

                    LogOutput.CommonLib?.Trace($"Loaded argument: {key} ({value})");
                }
                else if (args[i].StartsWith("-"))
                {
                    var switchName = args[i].Trim('-');

                    if (switches.Contains(switchName))
                    {
                        LogOutput.CommonLib?.Warn($"Failed to parse argument '{args[i]}' at {i}: this switch already exists");
                        continue;
                    }

                    switches.Add(switchName);

                    LogOutput.CommonLib?.Trace($"Loaded switch: {switchName}");
                }
                else
                {
                    LogOutput.CommonLib?.Warn($"Failed to parse argument '{args[i]}' at {i}");
                    continue;
                }
            }

            LogOutput.CommonLib?.Info($"Parsed {keys.Count} key(s) and {switches.Count} switch(es) from {args.Length} startup argument(s).");
        }

        public static bool HasSwitch(string switchName)
            => switches.Contains(switchName);

        public static string GetValue(string key)
            => keys.TryGetValue(key, out var value) ? value : string.Empty;
    }
}
