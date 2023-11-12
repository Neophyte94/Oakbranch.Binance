using System;
using System.Diagnostics;
using Oakbranch.Common.Logging;

namespace Oakbranch.Binance.Benchmark
{
    public sealed class ConsoleLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Info;

        private static int GetLevelPriority(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => 0,
                LogLevel.Warning => 1,
                LogLevel.Info => 2,
                LogLevel.Debug => 3,
                _ => int.MaxValue,
            };
        }

        public bool IsLevelEnabled(LogLevel level)
        {
            return GetLevelPriority(level) <= GetLevelPriority(Level);
        }

        public void Log(LogLevel level, string context, string message)
        {
            string msg = String.IsNullOrEmpty(context) ?
                $"[{DateTime.Now:HH:mm:ss.fff}]: {message}" :
                $"[{DateTime.Now:HH:mm:ss.fff}] [{context}]: {message}";
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }
    }
}
