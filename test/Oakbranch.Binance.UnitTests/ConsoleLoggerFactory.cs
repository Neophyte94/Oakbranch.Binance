using Microsoft.Extensions.Logging;

namespace Oakbranch.Binance.UnitTests;

public sealed class ConsoleLoggerFactory : ILoggerFactory
{
    private readonly LogLevel _level;
    private bool _isDisposed;

    public ConsoleLoggerFactory(LogLevel level)
    {
        _level = level;
    }

    public void AddProvider(ILoggerProvider provider)
    {
        return;
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        return new ConsoleLogger(categoryName) { Level = _level };
    }

    public void Dispose()
    {
        _isDisposed = true;
    }
}
