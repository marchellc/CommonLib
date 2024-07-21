using System;

namespace CommonLib.Logging
{
    public interface ILogger
    {
        DateTime Started { get; }

        LogMessage Latest { get; }

        void Emit(LogMessage message);
    }
}