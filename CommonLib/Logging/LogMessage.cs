using System;

namespace CommonLib.Logging
{
    public struct LogMessage
    {
        public LogCharacter[] Tag;
        public LogCharacter[] Time;
        public LogCharacter[] Source;
        public LogCharacter[] Message;

        public LogLevel Level;
        public LogOutput Output;

        public DateTime RequestTime;
        public DateTime ResponseTime;
    }
}