using CommonLib.Extensions;

using System;

namespace CommonLib.Logging
{
    public static class LogEvents
    {
        public static event Action<LogMessage> OnWritten;

        internal static void Invoke(LogMessage message) { OnWritten.Call(message); }
    }
}