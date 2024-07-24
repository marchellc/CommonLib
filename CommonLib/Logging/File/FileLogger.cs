using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonLib.Logging.File
{
    public static class FileLogger
    {
        private static string path;
        private static object gLock;

        private static List<LogMessage> toAdd;

        internal static void Init(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
                throw new ArgumentNullException(nameof(logPath));

            path = logPath;
            gLock = new object();
            toAdd = new List<LogMessage>();

            Task.Run(async () =>
            {
                while (true)
                {
                    WriteLogs();
                    await Task.Delay(500);
                }
            });
        }

        public static void Emit(LogMessage message)
        {
            lock (gLock)
                toAdd.Add(message);
        }

        private static void WriteLogs()
        {
            lock (gLock)
            {
                try
                {
                    System.IO.File.AppendAllLines(path, toAdd.Select(msg => msg.GetString()));
                }
                catch { }

                toAdd.Clear();
            }
        }
    }
}