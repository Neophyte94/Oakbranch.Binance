using System;

namespace Oakbranch.Binance.Models.Futures
{
    /// <summary>
    /// Represents information on open contracts on a symbol.
    /// </summary>
    public readonly struct OpenInterest
    {
        /// <summary>
        /// Defines the information timestamp.
        /// </summary>
        public readonly DateTime Timestamp;
        /// <summary>
        /// Defines the total quantity of open contracts.
        /// </summary>
        public readonly double TotalQuantity;
        /// <summary>
        /// Defines the total value of open contracts.
        /// <para>The value is <see cref="double.NaN"/> if unknown.</para>
        /// </summary>
        public readonly double TotalValue;

        /// <summary>
        /// Creates a new instance of the <see cref="OpenInterest"/> struct.
        /// </summary>
        /// <param name="timestamp">The information timestamp.</param>
        /// <param name="totalQuantity">The total quantity of open contracts.</param>
        /// <param name="totalValue">The total value of open contracts.</param>
        public OpenInterest(DateTime timestamp, double totalQuantity, double totalValue)
        {
            Timestamp = timestamp;
            TotalQuantity = totalQuantity;
            TotalValue = totalValue;
        }
    }
}
