using System;
using System.Collections.Generic;
using System.Threading;

namespace CommonLib.Logging.File
{
    public static class FileLogger
    {
        private static string path;
        private static object gLock;
        private static Timer timer;

        private static List<LogMessage> toAdd;

        internal static void Init(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
                throw new ArgumentNullException(nameof(logPath));

            path = logPath;
            gLock = new object();
            toAdd = new List<LogMessage>();
            timer = new Timer(_ => WriteLogs(), null, 10, 500);
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
                    foreach (var msg in toAdd)
                    {
                        var str = msg.GetString();

                        if (string.IsNullOrWhiteSpace(str))
                            continue;

                        System.IO.File.AppendAllLines(path, new string[] { str });
                    }
                }
                catch { }

                toAdd.Clear();
            }
        }
    }
}