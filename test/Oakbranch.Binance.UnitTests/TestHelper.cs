using System;

namespace Oakbranch.Binance.UnitTests;

public static class TestHelper
{
    public static (TimeSpan minInterval, TimeSpan maxInterval) ParseInterval(string intervalName)
    {
        if (String.IsNullOrWhiteSpace(intervalName))
        {
            throw new ArgumentNullException(nameof(intervalName));
        }

        char? firstDigit = intervalName.FirstOrDefault((c) => char.IsDigit(c));
        if (firstDigit == null)
        {
            throw new ArgumentException($"The interval \"{intervalName}\" contains no span specifier.");
        }

        int digitIdx = intervalName.IndexOf(firstDigit.Value);
        if (!int.TryParse(intervalName[digitIdx..], out int span))
        {
            throw new ArgumentException($"The interval specified \"{intervalName[digitIdx..]}\" is invalid.");
        }

        return (intervalName[..digitIdx].ToLowerInvariant()) switch
        {
            "millisecond" => new(
                new TimeSpan(TimeSpan.TicksPerMillisecond * span - TimeSpan.TicksPerMillisecond),
                new TimeSpan(TimeSpan.TicksPerMillisecond * span)),
            "second" => new(
                new TimeSpan(TimeSpan.TicksPerSecond * span - TimeSpan.TicksPerMillisecond),
                new TimeSpan(TimeSpan.TicksPerSecond * span)),
            "minute" => new(
                new TimeSpan(TimeSpan.TicksPerMinute * span - TimeSpan.TicksPerMillisecond),
                new TimeSpan(TimeSpan.TicksPerMinute * span)),
            "hour" => new(
                new TimeSpan(TimeSpan.TicksPerHour * span - TimeSpan.TicksPerMillisecond),
                new TimeSpan(TimeSpan.TicksPerHour * span)),
            "day" => new(
                new TimeSpan(TimeSpan.TicksPerDay * span - TimeSpan.TicksPerMillisecond),
                new TimeSpan(TimeSpan.TicksPerDay * span)),
            "week" => new(
                new TimeSpan(TimeSpan.TicksPerDay * 7 * span - TimeSpan.TicksPerMillisecond),
                new TimeSpan(TimeSpan.TicksPerDay * 7 * span)),
            "month" => new(
                new TimeSpan(TimeSpan.TicksPerDay * 28 * span - TimeSpan.TicksPerMillisecond),
                new TimeSpan(TimeSpan.TicksPerDay * 31 * span)),
            "year" => new(
                new TimeSpan(TimeSpan.TicksPerDay * 364 * span - TimeSpan.TicksPerMillisecond),
                new TimeSpan(TimeSpan.TicksPerDay * 365 * span)),
            _ => throw new ArgumentException($"The interval unit \"{intervalName[..digitIdx]}\" is unknown.")
        };
    }
}
