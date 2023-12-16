using System;
using System.Diagnostics;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Core.TimeProviders;

/// <summary>
/// Provides functionality for tracking and retrieving the estimated server time.
/// <para>The time estimation is based on the specified server time zone and last known server time.</para>
/// </summary>
public class ServerTimeProvider : ITimeProvider
{
    #region Instance props & fields

    private readonly Stopwatch _timeCounter;
    private readonly long _serverZoneOffset;
    private long _baseTime;

    /// <summary>
    /// Gets the estimated server time.
    /// </summary>
    public DateTime EstimatedServerTime
    {
        get
        {
            return new DateTime(_baseTime + _serverZoneOffset + _timeCounter.Elapsed.Ticks);
        }
    }

    /// <summary>
    /// Gets the estimated current UTC time.
    /// </summary>
    public DateTime UtcNow
    {
        get
        {
            return new DateTime(_baseTime + _timeCounter.Elapsed.Ticks);
        }
    }

    #endregion

    #region Instance constructors

    /// <summary>
    /// Creates a new instance of <see cref="ServerTimeProvider"/> with the specified parameters.
    /// </summary>
    /// <param name="serverTimeZone">The time zone of the server.</param>
    /// <param name="serverNow">The last known server time.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serverTimeZone"/> is null.</exception>
    public ServerTimeProvider(TimeZoneInfo serverTimeZone, DateTime serverNow)
    {
        ArgumentNullException.ThrowIfNull(serverTimeZone);

        _timeCounter = new Stopwatch();
        _serverZoneOffset = serverTimeZone.BaseUtcOffset.Ticks;
        SetServerNow(serverNow);
    }

    #endregion

    #region Instance methods

    /// <summary>
    /// Restarts the time tracking with the specified server time.
    /// </summary>
    /// <param name="serverNow">The last known server time.</param>
    public void SetServerNow(DateTime serverNow)
    {
        _baseTime = serverNow.Ticks - _serverZoneOffset;
        _timeCounter.Restart();
    }

    #endregion
}
