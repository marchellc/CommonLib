using CommonLib.Extensions;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CommonLib.Utilities.Console
{
    public static class ConsoleCommands
    {
        private static volatile Dictionary<string, Func<string[], string>> commands = new Dictionary<string, Func<string[], string>>();

        public static void Enable()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    OnUpdate();
                    await Task.Delay(100);
                }
            });
        }

        public static void Add(string cmd, Func<string[], string> callback)
            => commands[cmd] = callback;

        private static void OnUpdate()
        {
            try
            {
                var input = System.Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    return;

                var split = input.Split(' ');

                if (split.Length <= 0)
                    return;

                var cmd = split[0].ToLower();
                
                CommonLog.Create(CommonLog.LogLevel.None, CommonLog.LogType.CommandOutput, cmd.ToUpper(), null);

                if (!commands.TryGetValue(cmd, out var callback))
                {
                    CommonLog.Create(CommonLog.LogLevel.None, CommonLog.LogType.CommandOutput, cmd.ToUpper(), "No such command.");
                    return;
                }

                var output = callback.Call(split.Skip(1).ToArray(), ex => CommonLog.Error("Console API", $"Command execution failed!\n{ex}"));

                if (string.IsNullOrWhiteSpace(output))
                {
                    CommonLog.Create(CommonLog.LogLevel.None, CommonLog.LogType.CommandOutput, cmd.ToUpper(), "No output from command");
                    return;
                }

                CommonLog.Create(CommonLog.LogLevel.None, CommonLog.LogType.CommandOutput, cmd.ToUpper(), output);
            }
            catch (Exception ex)
            {
                CommonLog.Error("Console API", $"Command update loop caught an exception:\n{ex}");
            }
        }
    }
}
