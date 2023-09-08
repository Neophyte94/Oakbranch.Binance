using System;

namespace Oakbranch.Binance.Futures
{
    /// <summary>
    /// Represents information on a certain stats ratio between long and short positions for a symbol.
    /// </summary>
    public readonly struct LongShortRatio
    {
        /// <summary>
        /// Defines the information timestamp.
        /// </summary>
        public readonly DateTime Timestamp;
        /// <summary>
        /// Defines the stats value ratio between long and short positions.
        /// </summary>
        public readonly double Ratio;
        /// <summary>
        /// Defines the share of long positions in the stats structure.
        /// <para>The value is typically in the range [0.0 ; 1.0]</para>
        /// </summary>
        public readonly double Longs;
        /// <summary>
        /// Defines the share of short positions in the stats structure.
        /// <para>The value is typically in the range [0.0 ; 1.0]</para>
        /// </summary>
        public readonly double Shorts;

        /// <summary>
        /// Creates a new instance of the <see cref="LongShortRatio"/> struct.
        /// </summary>
        /// <param name="symbol">The futures contract symbol.</param>
        /// <param name="timestamp">The information timestamp.</param>
        /// <param name="ratio">The stats value ratio between long and short positions.</param>
        /// <param name="longs">The share of long positions in the stats structure.</param>
        /// <param name="shorts">The share of short positions in the stats structure.</param>
        public LongShortRatio(DateTime timestamp, double ratio, double longs, double shorts)
        {
            Timestamp = timestamp;
            Ratio = ratio;
            Longs = longs;
            Shorts = shorts;
        }
    }
}
