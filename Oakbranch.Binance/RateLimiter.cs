using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Represents settings of a rate limit on API calls.
    /// </summary>
    public struct RateLimiter
    {
        /// <summary>
        /// Defines the type of the rate limit.
        /// </summary>
        public RateLimitType Type;

        /// <summary>
        /// Defines the time unit of the interval of the rate limit window.
        /// </summary>
        public Interval Interval;

        /// <summary>
        /// Defines the number of time units in the interval of the rate limit window.
        /// </summary>
        public ushort IntervalNumber;

        /// <summary>
        /// Defines the maximum allowed usage of the rate limit.
        /// </summary>
        public uint Limit;

        /// <summary>
        /// Defines the current usage of the rate limit.
        /// </summary>
        public uint? Usage;

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimiter"/> struct with the specified rate limit settings.
        /// </summary>
        /// <param name="type">The type of rate limit.</param>
        /// <param name="interval">The interval at which the rate limit applies.</param>
        /// <param name="intervalNumber">The number of intervals within the rate limit window.</param>
        /// <param name="limit">The maximum allowed limit for the rate limit window.</param>
        /// <param name="usage">The current usage of the rate limit. Use the <c>Null</c> value if not available.</param>
        public RateLimiter(RateLimitType type, Interval interval, ushort intervalNumber, uint limit, uint? usage = null)
        {
            Type = type;
            Interval = interval;
            IntervalNumber = intervalNumber;
            Limit = limit;
            Usage = usage;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="RateLimiter"/> instance.
        /// </summary>
        /// <returns>A string representation of the <see cref="RateLimiter"/> instance.</returns>
        public override string ToString()
        {
            return $"Rate limter: Type = {Type}, Interval = {IntervalNumber}-{Interval}, Limit = {Limit}, Usage = {Usage}";
        }
    }
}
