using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MoqConverter.Core
{
    public static class Logger
    {
        private class LogModel
        {
            public string Message { get; set; }
            public ConsoleColor Color { get; set; }
        }

        private static readonly BlockingCollection<LogModel> Logs = new BlockingCollection<LogModel>();

        public static void Log(string message, ConsoleColor consoleColor = ConsoleColor.Magenta)
        {
            Logs.Add(new LogModel
            {
                Message = message,
                Color = consoleColor
            });
        }

        public static void Init()
        {
            Task.Run(() =>
            {
                InitLogs();
            });
        }

        private static void InitLogs()
        {
            try
            {
                foreach (var log in Logs.GetConsumingEnumerable())
                {
                    Console.ForegroundColor = log.Color;
                    Console.WriteLine(log.Message);
                    Console.ResetColor();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }
    }
}
