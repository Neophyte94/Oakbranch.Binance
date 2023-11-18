using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Oakbranch.Binance.UnitTests;

public sealed class ConsoleLogger : ILogger
{
    #region Nested types

    private sealed class LoggingScope : IDisposable
    {
        private LoggingScope? _parent;
        private LoggingScope? _child;
        private readonly string _name;

        public LoggingScope(string? name) : this(null, name) { }

        private LoggingScope(LoggingScope? parent, string? name)
        {
            _parent = parent;
            _name = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        }

        public static IDisposable BeginNested(LoggingScope root, string? name)
        {
            while (root._child != null)
            {
                root = root._child;
            }

            LoggingScope child = new LoggingScope(root, name);
            root._child = child;

            return child;
        }

        public string? GetCurrentContext()
        {
            string context = _name;
            LoggingScope? scope = _child;

            while (scope != null)
            {
                context = context.Length != 0 ? $"{context}.{scope._name}" : scope._name;
                scope = scope._child;
            }

            return _name.Length != 0 ? _name : null;
        }

        public void Dispose()
        {
            if (_parent != null)
            {
                _parent._child = null;
                _parent = null;
            }
        }
    }

    #endregion

    #region Instance members

    private readonly LoggingScope _rootScope;

    public LogLevel Level { get; set; } = LogLevel.Information;

    #endregion

    #region Instance constructors

    public ConsoleLogger(string? rootScope)
    {
        _rootScope = new LoggingScope(rootScope);
    }

    #endregion

    #region Instance methods

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return LoggingScope.BeginNested(_rootScope, state.ToString());
    }

    public bool IsEnabled(LogLevel level)
    {
        return level <= Level;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            string msg = $"[{DateTime.Now:HH:mm:ss.fff}]";

            string? context = _rootScope.GetCurrentContext();
            if (context != null)
            {
                msg += $" [{context}]";
            }

            if (eventId != default)
            {
                msg += $" [{eventId}]";
            }
            
            msg += $": {formatter(state, exception)}";

            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }
    }

    #endregion
}
