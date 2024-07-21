using System;

namespace CommonLib.Logging
{
    [Flags]
    public enum LogLevel : byte
    {
        Trace = 2,
        Debug = 4,
        Verbose = 8,
        Information = 16,
        Warning = 32,
        Error = 64,
        Fatal = 128
    }
}