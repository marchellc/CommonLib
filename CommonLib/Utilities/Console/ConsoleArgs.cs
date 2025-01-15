using System;
using System.Collections.Generic;

namespace CommonLib.Utilities.Console
{
    public static class ConsoleArgs
    {
        private static volatile Dictionary<string, string> keys = new Dictionary<string, string>();
        private static volatile HashSet<string> switches = new HashSet<string>();

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
                        CommonLog.Warn("Console API", $"Failed to parse argument {args[i]}");
                        continue;
                    }

                    var split = args[i].Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length != 2)
                    {
                        CommonLog.Warn("Console API", $"Failed to parse argument {args[i]}");
                        continue;
                    }

                    var key = split[0].Replace("--", "").Trim();
                    var value = split[1].Trim();

                    if (keys.ContainsKey(key))
                    {
                        CommonLog.Warn("Console API", $"Failed to parse argument {args[i]}");
                        continue;
                    }

                    keys[key] = value;
                }
                else if (args[i].StartsWith("-"))
                {
                    var switchName = args[i].Trim('-');

                    if (switches.Contains(switchName))
                    {
                        CommonLog.Warn("Console API", $"Failed to parse argument {args[i]}");
                        continue;
                    }

                    switches.Add(switchName);
                }
            }
        }

        public static bool HasSwitch(string switchName)
            => switches.Contains(switchName);
        
        public static bool HasValue(string key)
            => keys.ContainsKey(key);

        public static string GetValue(string key)
            => keys.TryGetValue(key, out var value) ? value : string.Empty;
        
        public static string GetValueOrDefault(string key, string defaultValue = "")
            => keys.TryGetValue(key, out var value) ? value : defaultValue;
        
        public static T GetValue<T>(string key, Func<string, T> converter)
            => keys.TryGetValue(key, out var value) ? converter(value) : throw new KeyNotFoundException(key);
        
        public static T GetValueOrDefault<T>(string key, Func<string, T> converter, T defaultValue)
            => keys.TryGetValue(key, out var value) ? converter(value) : defaultValue;
    }
}
