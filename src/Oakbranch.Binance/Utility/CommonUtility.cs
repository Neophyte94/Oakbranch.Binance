using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Oakbranch.Binance.Core;

namespace Oakbranch.Binance.Utility;

internal static class CommonUtility
{
    #region Static members

    public static readonly NumberFormatInfo NumberFormat;

    private static readonly DateTime s_apiBaseDate = new DateTime(1970, 1, 1);
    public static DateTime ApiBaseDate => s_apiBaseDate;

    #endregion

    #region Static constructor

    static CommonUtility()
    {
        NumberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        NumberFormat.NumberGroupSeparator = string.Empty;
        NumberFormat.CurrencyGroupSeparator = string.Empty;
        NumberFormat.PercentGroupSeparator = string.Empty;
        NumberFormat.NumberDecimalSeparator = ".";
    }

    #endregion

    #region Static methods

    /// <summary>
    /// Normalizes the given string representing an exchange symbol to the format accepted by the Binance API.
    /// </summary>
    /// <param name="symbol">The exchange symbol to be normalized.</param>
    /// <returns>The exchange symbol adapted to the format accepted by the exchange API.</returns>
    [return: NotNullIfNotNull(nameof(symbol))]
    public static string? NormalizeSymbol(string? symbol)
    {
        return symbol?.ToUpperInvariant();
    }

    /// <summary>
    /// Converts the <see cref="long"/> value representing time in the Binance API format
    /// to a corresponding <see cref="DateTime"/> instance.
    /// </summary>
    /// <param name="ms">The time value in the Binance API format to convert.</param>
    /// <returns>A <see cref="DateTime"/> instance corresponding to the specified time.</returns>
    public static DateTime ConvertToDateTime(long ms)
    {
        return s_apiBaseDate + TimeSpan.FromMilliseconds(ms);
    }

    /// <summary>
    /// Converts the given date &amp; time to a <see cref="long"/> value in the Binance API format.
    /// </summary>
    /// <param name="value">The date &amp; time to convert.</param>
    /// <returns>A <see cref="long"/> value corresponding to the specified time in the Binance API format.</returns>
    public static long ConvertToApiTime(DateTime value)
    {
        return (long)(value - ApiBaseDate).TotalMilliseconds;
    }

    /// <summary>
    /// Decodes a byte array content to a string, using ASCII encoding.
    /// <para>If decoding fails, returns a formatted string of the HEX representation of the byte array.</para>
    /// </summary>
    /// <param name="content">The byte array content to be decode.</param>
    /// <returns>A string representing the decoded content, or the HEX representation of the byte content.</returns>
    public static string? DecodeByteContent(byte[]? content)
    {
        if (content == null || content.Length == 0)
        {
            return null;
        }
        try
        {
            return Encoding.ASCII.GetString(content);
        }
        catch
        {
            return $"{{ {string.Join(", ", content.Select((b) => b.ToString("2X")))} }}";
        }
    }

    /// <summary>
    /// Finds the index of the first empty or whitespace string within the given array.
    /// </summary>
    /// <param name="items">The array of strings to search through.</param>
    /// <returns>The index of the first empty or whitespace string, or -1 if none exists.</returns>
    public static int IndexOfEmpty(string[] items)
    {
        if (items == null) return -1;

        for (int i = 0; i != items.Length; ++i)
        {
            if (string.IsNullOrWhiteSpace(items[i]))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Creates a <see cref="TimeSpan"/> instance based on the specified interval.
    /// </summary>
    /// <param name="interval">The interval type to create a timespan from.</param>
    /// <param name="num">The number of intervals to create a timespan from.</param>
    /// <returns>A <see cref="TimeSpan"/> instance representing the specified interval.</returns>
    public static TimeSpan CreateTimespan(Interval interval, int num)
    {
        return interval switch
        {
            Interval.Day => new TimeSpan(num * TimeSpan.TicksPerDay),
            Interval.Hour => new TimeSpan(num * TimeSpan.TicksPerHour),
            Interval.Minute => new TimeSpan(num * TimeSpan.TicksPerMinute),
            Interval.Second => new TimeSpan(num * TimeSpan.TicksPerSecond),
            _ => throw new NotImplementedException($"The interval \"{interval}\" is unknown."),
        };
    }

    /// <summary>
    /// Gets a natural-language description of the given timespan.
    /// </summary>
    /// <param name="interval">The timespan to describe.</param>
    /// <returns>A string representing the duration of the given timespan (in terms of days, hours, etc).</returns>
    public static string GetIntervalDescription(TimeSpan interval)
    {
        if (interval.Ticks >= TimeSpan.TicksPerDay)
        {
            return $"{interval.TotalDays}-day";
        }
        else if (interval.Ticks >= TimeSpan.TicksPerHour)
        {
            return $"{interval.TotalHours}-hour";
        }
        else if (interval.Ticks >= TimeSpan.TicksPerMinute)
        {
            return $"{interval.TotalMinutes}-minute";
        }
        else if (interval.Ticks >= TimeSpan.TicksPerSecond)
        {
            return $"{interval.TotalSeconds}-second";
        }
        else
        {
            return $"{interval.TotalMilliseconds}-ms";
        }
    }

    /// <summary>
    /// Traverses the path hierarchy of a URL endpoint and returns each segment of the hierarchy,
    /// starting from the full original endpoint.
    /// </summary>
    /// <param name="endpoint">The URL endpoint to traverse.</param>
    /// <returns>An enumerable sequence of strings representing each segment of the path hierarchy, in the reverse order.</returns>
    public static IEnumerable<string> TraversePathHierarchy(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            yield break;
        }

        yield return endpoint;

        while (true)
        {
            int slashIdx = endpoint.LastIndexOf('/', endpoint.Length - 1, endpoint.Length);
            if (slashIdx < 1) yield break;

            endpoint = endpoint[..slashIdx];
            yield return endpoint;
        }
    }

    /// <summary>
    /// Gets the deterministic hash code for the specified string value.
    /// <para>The deterministic result is independent of the processor architecture of the executing machine.</para>
    /// </summary>
    /// <param name="value">The string value to calculate the hash code for.</param>
    /// <returns>The calculated deterministic hash code for the input string.</returns>
    public static int GetDeterministicHashCode(this string value)
    {
        unchecked
        {
            int hash1 = (2386 << 16) + 5690;
            int hash2 = hash1;

            for (int i = 0; i != value.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ value[i];
                if (i == value.Length - 1)
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ value[i + 1];
            }

            return hash1 + (hash2 * 92456655);
        }
    }

    #endregion
}
