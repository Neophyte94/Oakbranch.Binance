using System;

namespace Oakbranch.Binance.Core.RateLimits
{
    /// <summary>
    /// Represents information on a rate limit.
    /// </summary>
    public readonly struct RateLimitInfo
    {
        /// <summary>
        /// Defines an identifier of a weight dimension to which a rate limit applies.
        /// </summary>
        public readonly int DimensionId;
        /// <summary>
        /// Defines a reset interval of a rate limit.
        /// <para>The value <see cref="TimeSpan.Zero"/> specifies that a rate limit has no automatic resets of usage level.</para>
        /// </summary>
        public readonly TimeSpan Interval;
        /// <summary>
        /// Gets the maximum level of a rate limit usage.
        /// </summary>
        public readonly uint Limit;
        /// <summary>
        /// Gets the current level of a rate limit usage.
        /// </summary>
        public readonly uint Usage;
        /// <summary>
        /// Defines a descriptive name of a rate limit.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets a value indicating whether this instance of <see cref="RateLimitInfo"/> represents an empty value.
        /// </summary>
        public bool IsUndefined => DimensionId == 0 && Limit == 0 && Name == null;

        /// <summary>
        /// Creates a new instance of the <see cref="RateLimitInfo"/> struct with the specified fields' values.
        /// </summary>
        /// <param name="dimensionId">An identifier of a target weight dimension of a rate limit.</param>
        /// <param name="interval">
        /// A reset interval of a rate limit.
        /// <para>Use the value <see cref="TimeSpan.Zero"/> to specify that a rate limit has no automatic resets.</para>
        /// </param>
        /// <param name="limit">The maximum level of a rate limit usage.</param>
        /// <param name="usage">The current level of a rate limit usage.</param>
        /// <param name="name">A descriptive name of a rate limit.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public RateLimitInfo(int dimensionId, TimeSpan interval, uint limit, uint usage, string name)
        {
            if (interval.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(interval));
            DimensionId = dimensionId;
            Interval = interval;
            Limit = limit;
            Usage = usage;
            Name = name;
        }
    }
}
