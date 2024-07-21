using CommonLib.Logging;
using CommonLib.Extensions;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommonLib.Utilities
{
    public static class ConsoleCommands
    {
        private static LogOutput log;
        private static Dictionary<string, Func<string[], string>> commands = new Dictionary<string, Func<string[], string>>();

        public static void Enable()
        {
            if (log != null)
                return;

            log = new LogOutput("Command Manager");
            log.Setup();

            Task.Run(async () =>
            {
                while (true)
                {
                    OnUpdate();
                    await Task.Delay(100);
                }
            });

            log.Info($"Commands enabled.");
        }

        public static void Add(string cmd, Func<string[], string> callback)
            => commands[cmd] = callback;

        private static void OnUpdate()
        {
            try
            {
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    return;

                var split = input.Split(' ');

                if (split.Length <= 0)
                    return;

                var cmd = split[0].ToLower();

                LogOutput.Raw($">>> {cmd.ToUpper()}", ConsoleColor.Magenta);

                if (!commands.TryGetValue(cmd, out var callback))
                {
                    LogOutput.Raw(">>> No such command.", ConsoleColor.Red);
                    return;
                }

                var output = callback.Call(split.Skip(1).ToArray(), ex => log.Error($"Command execution failed!\n{ex}"));

                if (string.IsNullOrWhiteSpace(output))
                {
                    LogOutput.Raw(">>> No output from command.", ConsoleColor.DarkYellow);
                    return;
                }

                LogOutput.Raw($">>> {output}", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                log.Error($"Command update loop caught an exception:\n{ex}");
            }
        }
    }
}
