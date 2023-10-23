using System;
using System.Diagnostics;
using Oakbranch.Common.Logging;

namespace Oakbranch.Binance.UnitTests
{
    public sealed class ConsoleLogger : ILogger
    {
        #region Instance members

        public LogLevel Level { get; set; } = LogLevel.Info;

        #endregion

        #region Static methods

        private static int GetLevelPriority(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error: return 0;
                case LogLevel.Warning: return 1;
                case LogLevel.Info: return 2;
                case LogLevel.Debug: return 3;
                default: return int.MaxValue;
            }
        }

        #endregion

        #region Instance methods

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

        #endregion
    }
}
