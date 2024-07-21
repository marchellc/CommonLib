using CommonLib.Logging.Console;

using System;

using CommonLib.Extensions;

namespace CommonLib.Logging
{
    public static class LogUtils
    {
        public const LogLevel General = LogLevel.Information | LogLevel.Warning | LogLevel.Error | LogLevel.Fatal;
        public const LogLevel Debug = LogLevel.Debug | LogLevel.Trace | LogLevel.Verbose;

        public static LogLevel Default;

        public static string Time
        {
            get
            {
                var time = DateTime.Now;
                return $"{time.Hour}:{time.Minute}:{time.Second}:{time.Millisecond}";
            }
        }

        public static LogOutput Setup(this LogOutput output)
        {
            if (LogOutput.CommonLib != null)
            {
                var loggers = LogOutput.CommonLib.GetLoggers();

                for (int i = 0; i < loggers.Length; i++)
                {
                    if (loggers[i] is ConsoleLogger)
                        continue;

                    output.AddLogger(loggers[i]);
                }
            }

            return output;
        }

        public static LogOutput Enable(this LogOutput output, LogLevel level)
        {
            if (output.Enabled.Any(level))
                return output;

            output.Enabled = output.Enabled.Combine(level);
            return output;
        }

        public static LogOutput Disable(this LogOutput output, LogLevel level)
        {
            if (!output.Enabled.Any(level))
                return output;

            output.Enabled = output.Enabled.Remove(level);
            return output;
        }

        public static LogMessage CreateMessage(string source, string message, LogLevel level)
        {
            var color = ColorOfLevel(level);
            var inverse = InverseColorOfLevel(level);
            var timeArray = CreateTag(Time, color, inverse);
            var levelArray = CreateTag(NameOfLevel(level), color, inverse);
            var sourceArray = CreateTag(source, color, inverse);

            var msgArray = new LogCharacter[message.Length];

            for (int i = 0; i < message.Length; i++)
                msgArray[i] = new LogCharacter(message[i], level > LogLevel.Warning ? color : ConsoleColor.White);

            return new LogMessage
            {
                Level = level,

                RequestTime = DateTime.Now,
                ResponseTime = DateTime.Now,

                Message = msgArray,
                Source = sourceArray,
                Tag = levelArray,
                Time = timeArray
            };
        }

        public static LogCharacter[] CreateTag(string tag, LogLevel level)
        {
            var chars = new LogCharacter[tag.Length + 2];

            var color = ColorOfLevel(level);
            var inverse = InverseColorOfLevel(level);

            chars[0] = new LogCharacter('[', inverse);
            chars[chars.Length - 1] = new LogCharacter(']', inverse);

            for (int i = 0; i < tag.Length; i++)
                chars[i + 1] = new LogCharacter(tag[i], color);

            return chars;
        }

        public static LogCharacter[] CreateTag(string tag, ConsoleColor color, ConsoleColor inverse)
        {
            var chars = new LogCharacter[tag.Length + 2];

            chars[0] = new LogCharacter('[', inverse);
            chars[chars.Length - 1] = new LogCharacter(']', inverse);

            for (int i = 0; i < tag.Length; i++)
                chars[i + 1] = new LogCharacter(tag[i], color);

            return chars;
        }

        public static string GetString(this LogCharacter[] chars)
        {
            var str = "";

            for (int i = 0; i < chars.Length; i++)
                str += $"{chars[i].Character}";

            return str;
        }

        public static string GetString(this LogMessage message, bool includeTime = true, bool includeLevel = true, bool includeSource = true)
        {
            var str = "";

            if (includeTime && message.Time != null && message.Time.Length > 0)
                str += message.Time.GetString() + " ";

            if (includeLevel && message.Tag != null && message.Tag.Length > 0)
                str += message.Tag.GetString() + " ";

            if (includeSource && message.Source != null && message.Source.Length > 0)
                str += message.Source.GetString() + " ";

            if (message.Message != null && message.Message.Length > 0)
                str += message.Message.GetString() + " ";

            return str;
        }

        public static string NameOfLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    return "FATAL";

                case LogLevel.Error:
                    return "ERROR";

                case LogLevel.Warning:
                    return "WARN";

                case LogLevel.Information:
                    return "INFO";

                case LogLevel.Verbose:
                    return "VERBOSE";

                case LogLevel.Debug:
                    return "DEBUG";

                case LogLevel.Trace:
                    return "TRACE";

                default:
                    return "??";
            }
        }

        // text
        public static ConsoleColor ColorOfLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    return ConsoleColor.DarkRed;

                case LogLevel.Error:
                    return ConsoleColor.White;

                case LogLevel.Warning:
                    return ConsoleColor.Yellow;

                case LogLevel.Information:
                    return ConsoleColor.White;

                case LogLevel.Verbose:
                    return ConsoleColor.White;

                case LogLevel.Debug:
                    return ConsoleColor.White;

                case LogLevel.Trace:
                    return ConsoleColor.White;

                default:
                    return ConsoleColor.White;
            }
        }

        // brackets
        public static ConsoleColor InverseColorOfLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    return ConsoleColor.Red;

                case LogLevel.Error:
                    return ConsoleColor.DarkRed;

                case LogLevel.Warning:
                    return ConsoleColor.DarkYellow;

                case LogLevel.Information:
                    return ConsoleColor.DarkGreen;

                case LogLevel.Verbose:
                    return ConsoleColor.Cyan;

                case LogLevel.Debug:
                    return ConsoleColor.DarkBlue;

                case LogLevel.Trace:
                    return ConsoleColor.DarkMagenta;

                default:
                    return ConsoleColor.Magenta;
            }
        }
    }
}
