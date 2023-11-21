using System;
using System.Diagnostics;
using Oakbranch.Binance.Utility;

namespace Oakbranch.Binance.Core.RateLimits;

/// <summary>
/// A thread-safe implementation of a rate limit tracker.
/// <para>The class encapsulates the logic for limit usage modification and automatic usage resets.</para>
/// </summary>
public class LimitCounter
{
    #region Instance props & fields

    /// <summary>
    /// Defines the identifier of a limit.
    /// </summary>
    public readonly int Id;
    /// <summary>
    /// Defines the identifier of the weight dimension targeted by a limit.
    /// </summary>
    public readonly int DimensionId;
    /// <summary>
    /// Defines the time interval that <see cref="Usage"/> is reseted at.
    /// </summary>
    public readonly TimeSpan ResetInterval;
    /// <summary>
    /// Defines a limit's descriptive name.
    /// </summary>
    public readonly string Name;

    private readonly object _locker;
    private readonly Stopwatch _resetTimer;
    private DateTime _lastUpdateTime;

    private uint _limit;
    /// <summary>
    /// Defines the maximum permitted level of limit usage.
    /// </summary>
    public uint Limit
    {
        get
        {
            return _limit;
        }
        set
        {
            _limit = value;
        }
    }

    private uint _usage;
    /// <summary>
    /// Gets the current level of limit usage.
    /// </summary>
    public uint Usage
    {
        get
        {
            CheckResetTimer();
            return _usage;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current usage level exceeds the usage limit.
    /// </summary>
    public bool IsViolated => _usage >= _limit;

    #endregion

    #region Instance constructors

    /// <summary>
    /// Creates an instance of the <see cref="LimitCounter"/> class using the specified limit parameters.
    /// </summary>
    public LimitCounter(
        int id,
        int dimensionId,
        uint limit,
        TimeSpan resetInterval,
        uint? usage = null,
        string? name = null)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
        if (resetInterval.Ticks < TimeSpan.TicksPerSecond)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resetInterval),
                $"The specified value of the reset interval ({resetInterval}) is invalid. " +
                "The limit reset interval must be at least 1 second long.");
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"{CommonUtility.GetIntervalDescription(resetInterval)} interval on the dimension {dimensionId}";
        }

        Id = id;
        DimensionId = dimensionId;
        Limit = limit;
        ResetInterval = resetInterval;
        Name = name;

        _locker = new object();
        _resetTimer = Stopwatch.StartNew();
        if (usage != null)
        {
            _usage = usage.Value;
        }
    }

    #endregion

    #region Instance methods

    /// <summary>
    /// Checks the specified amount of additional limit usage against the limit.
    /// <para>Returns true if there's enough gap between the current usage and the limit; otherwise returns false.</para>
    /// </summary>
    /// <param name="extra">Extra usage amount to check against.</param>
    /// <returns></returns>
    public bool TestUsage(uint extra)
    {
        CheckResetTimer();
        return _usage + extra < _limit;
    }

    public void AddUsage(uint points, DateTime timestamp)
    {
        CheckResetTimer();
        lock (_locker)
        {
            _usage += points;
            if (timestamp > _lastUpdateTime)
            {
                _lastUpdateTime = timestamp;
            }
        }
    }

    public void SetUsage(uint points, DateTime timestamp)
    {
        bool wasTimerReset = CheckResetTimer();

        bool wasUsageReset = false;
        lock (_locker)
        {
            if (timestamp >= _lastUpdateTime)
            {
                wasUsageReset = points < _usage;
                _usage = points;
                _lastUpdateTime = timestamp;
            }
        }

        if (wasUsageReset && !wasTimerReset)
        {
            _resetTimer.Restart();
        }
    }

    private bool CheckResetTimer()
    {
        if (_resetTimer.Elapsed >= ResetInterval)
        {
            lock (_locker) { _usage = 0; }
            _resetTimer.Restart();
            return true;
        }
        else
        {
            return false;
        }
    }

    public override string ToString() => Name;

    #endregion
}
