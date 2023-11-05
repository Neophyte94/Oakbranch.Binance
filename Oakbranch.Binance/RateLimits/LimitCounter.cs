using System;
using System.Diagnostics;

namespace Oakbranch.Binance.RateLimits;

/// <summary>
/// A thread-safe implementation of a rate limit tracker.
/// <para>The class encapsulates the logic for limit usage modification and automatic usage resets.</para>
/// </summary>
public class LimitCounter
{
    #region Instance members

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

    private readonly object m_Locker;
    private readonly Stopwatch m_ResetTimer;
    private DateTime m_LastUpdateTime;

    private uint m_Limit;
    /// <summary>
    /// Defines the maximum permitted level of limit usage.
    /// </summary>
    public uint Limit
    {
        get
        {
            return m_Limit;
        }
        set
        {
            m_Limit = value;
        }
    }

    private uint m_Usage;
    /// <summary>
    /// Gets the current level of limit usage.
    /// </summary>
    public uint Usage
    {
        get
        {
            CheckResetTimer();
            return m_Usage;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current usage level exceeds the usage limit.
    /// </summary>
    public bool IsViolated => m_Usage >= m_Limit;

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
        if (String.IsNullOrWhiteSpace(name))
        {
            name = $"{CommonUtility.GetIntervalDescription(resetInterval)} interval on the dimension {dimensionId}";
        }

        Id = id;
        DimensionId = dimensionId;
        Limit = limit;
        ResetInterval = resetInterval;
        Name = name;

        m_Locker = new object();
        m_ResetTimer = Stopwatch.StartNew();
        if (usage != null)
        {
            m_Usage = usage.Value;
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
        return m_Usage + extra < m_Limit;
    }

    public void AddUsage(uint points, DateTime timestamp)
    {
        CheckResetTimer();
        lock (m_Locker)
        {
            m_Usage += points;
            if (timestamp > m_LastUpdateTime)
            {
                m_LastUpdateTime = timestamp;
            }
        }
    }

    public void SetUsage(uint points, DateTime timestamp)
    {
        bool wasTimerReset = CheckResetTimer();

        bool wasUsageReset = false;
        lock (m_Locker)
        {
            if (timestamp >= m_LastUpdateTime)
            {
                wasUsageReset = points < m_Usage;
                m_Usage = points;
                m_LastUpdateTime = timestamp;
            }
        }

        if (wasUsageReset && !wasTimerReset)
        {
            m_ResetTimer.Restart();
        }
    }

    private bool CheckResetTimer()
    {
        if (m_ResetTimer.Elapsed >= ResetInterval)
        {
            lock (m_Locker) { m_Usage = 0; }
            m_ResetTimer.Restart();
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
