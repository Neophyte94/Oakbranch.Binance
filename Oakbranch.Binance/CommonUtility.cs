using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Oakbranch.Binance
{
    internal static class CommonUtility
    {
        #region Static members

        public static readonly NumberFormatInfo NumberFormat;

        private static readonly DateTime s_ApiBaseDate = new DateTime(1970, 1, 1);
        public static DateTime ApiBaseDate => s_ApiBaseDate;

        #endregion

        #region Static constructor

        static CommonUtility()
        {
            NumberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            NumberFormat.NumberGroupSeparator = String.Empty;
            NumberFormat.CurrencyGroupSeparator = String.Empty;
            NumberFormat.PercentGroupSeparator = String.Empty;
            NumberFormat.NumberDecimalSeparator = ".";
        }

        #endregion

        #region Static methods

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            else if (value > max) return max;
            else return value;
        }

        public static string DecodeByteContent(byte[] content)
        {
            if (content == null || content.Length == 0) return null;
            try { return Encoding.ASCII.GetString(content); }
            catch { return $"{{ {String.Join(", ", content.Select((b) => b.ToString("2X")))} }}"; }
        }

        public static DateTime ConvertToDateTime(long ms)
        {
            return s_ApiBaseDate + TimeSpan.FromMilliseconds(ms);
        }

        public static long ConvertToApiTime(DateTime value)
        {
            return (long)(value - ApiBaseDate).TotalMilliseconds;
        }

        public static int IndexOfEmpty(string[] items)
        {
            if (items == null) return -1;

            for (int i = 0; i != items.Length; ++i)
            {
                if (String.IsNullOrWhiteSpace(items[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static TimeSpan CreateTimespan(Interval interval, int num)
        {
            switch (interval)
            {
                case Interval.Day:
                    return new TimeSpan(num * TimeSpan.TicksPerDay);
                case Interval.Hour:
                    return new TimeSpan(num * TimeSpan.TicksPerHour);
                case Interval.Minute:
                    return new TimeSpan(num * TimeSpan.TicksPerMinute);
                case Interval.Second:
                    return new TimeSpan(num * TimeSpan.TicksPerSecond);
                default:
                    throw new NotImplementedException($"The interval \"{interval}\" is unknown.");
            }
        }

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
            if (String.IsNullOrWhiteSpace(endpoint)) yield break;

            yield return endpoint;

            while (true)
            {
                int slashIdx = endpoint.LastIndexOf('/', endpoint.Length - 1, endpoint.Length);
                if (slashIdx < 1) yield break;

                endpoint = endpoint.Substring(0, slashIdx);
                yield return endpoint;
            }
        }

        #endregion
    }
}
