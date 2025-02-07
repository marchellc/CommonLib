using System;

using CommonLib;

using System.Threading.Tasks;

namespace CommonTest
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Task.Run(UpdateLogs);
            
            CommonLibrary.Initialize(args);

            await Task.Delay(-1);
        }

        private static void UpdateLogs()
        {
            void Write(string line, ConsoleColor color)
            {
                Console.ForegroundColor = color;
                Console.Write(line);
                Console.ResetColor();
            }
            
            while (true)
            {
                try
                {
                    var now = DateTime.Now;
                    var time = $"{now.Hour.ToString("00")}:{now.Minute.ToString("00")}";
                    
                    while (CommonLog.Logs.TryDequeue(out var log))
                    {
                        if (log.Type is CommonLog.LogType.RawOutput)
                        {
                            Console.WriteLine(log.Message);
                            continue;
                        }
                        
                        if (string.IsNullOrWhiteSpace(log.Message) || string.IsNullOrWhiteSpace(log.Source))
                            continue;
                        
                        var tagColor = ConsoleColor.White;
                        var textColor = ConsoleColor.White;
                        var timeColor = ConsoleColor.White;

                        var tagText = string.Empty;
                        var textText = string.Empty;

                        if (log.Type is CommonLog.LogType.CommandOutput)
                        {
                            Write($"{log.Source} >>> ", ConsoleColor.Magenta);
                            Write(log.Message, ConsoleColor.White);
                            
                            Console.WriteLine();
                        }
                        else
                        {
                            switch (log.Level)
                            {
                                case CommonLog.LogLevel.Debug:
                                    tagColor = ConsoleColor.Cyan;
                                    textColor = ConsoleColor.Gray;
                                    timeColor = ConsoleColor.Cyan;
                                    break;
                                
                                case CommonLog.LogLevel.Error:
                                    tagColor = ConsoleColor.Red;
                                    textColor = ConsoleColor.Yellow;
                                    timeColor = ConsoleColor.Red;
                                    break;
                                
                                case CommonLog.LogLevel.Warn:
                                    tagColor = ConsoleColor.Yellow;
                                    textColor = ConsoleColor.Yellow;
                                    timeColor = ConsoleColor.Yellow;
                                    break;
                                
                                case CommonLog.LogLevel.Info:
                                    tagColor = ConsoleColor.Green;
                                    textColor = ConsoleColor.White;
                                    timeColor = ConsoleColor.Green;
                                    break;
                                    
                                default:
                                    continue;
                            }
                            
                            Write($"[{time}]", timeColor);
                            Write($" [{log.Source}] ", tagColor);
                            Write(log.Message, textColor);
                            
                            Console.WriteLine();
                        }
                    }
                }
                catch { }
            }
        }
    }
}