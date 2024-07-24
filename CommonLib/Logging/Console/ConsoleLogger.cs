using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CommonLib.Logging.Console
{
    public class ConsoleLogger : ILogger
    {
        public static readonly ConsoleLogger Instance = new ConsoleLogger();

        private LogMessage lastMsg;
        private ConcurrentQueue<LogMessage> messages;
        private object queueLock;

        public LogMessage Latest => lastMsg;

        public DateTime Started { get; }

        public ConsoleLogger()
        {
            Started = DateTime.Now;

            queueLock = new object();
            messages = new ConcurrentQueue<LogMessage>();

            Task.Run(async () =>
            {
                while (true)
                {
                    UpdateQueue();
                    await Task.Delay(50);
                }
            });
        }

        public void Emit(LogMessage message)
        {
            lock (queueLock)
                messages.Enqueue(message);
        }

        private void Show(LogMessage message)
        {
            lastMsg = message;

            try
            {
                if (message.Time != null && message.Time.Length > 0)
                    WriteArray(message.Time);

                if (message.Tag != null && message.Tag.Length > 0)
                    WriteArray(message.Tag);

                if (message.Source != null && message.Source.Length > 0)
                    WriteArray(message.Source);

                if (message.Message != null && message.Message.Length > 0)
                    WriteArray(message.Message);

                System.Console.WriteLine();
            }
            catch { }
        }

        private void UpdateQueue()
        {
            lock (queueLock)
            {
                while (messages.TryDequeue(out var message))
                    Show(message);
            }
        }

        private static void WriteArray(LogCharacter[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (System.Console.ForegroundColor != array[i].Color)
                    System.Console.ForegroundColor = array[i].Color;

                System.Console.Write(array[i].Character);
                System.Console.ResetColor();
            }

            System.Console.Write(' ');
        }
    }
}