using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Represents a footprint of a query on a specific weight dimension of rate limits.
    /// </summary>
    public readonly struct QueryWeight
    {
        /// <summary>
        /// Defines an identifier of a weight dimension.
        /// </summary>
        public readonly int DimensionId;
        /// <summary>
        /// Defines a numeric value of a query's footprint on the target weight dimension.
        /// </summary>
        public readonly uint Amount;

        /// <summary>
        /// Creates a new instance of the <see cref="QueryWeight"/> struct with the specified parameters.
        /// </summary>
        /// <param name="dimensionId">A weight dimension identifier.</param>
        /// <param name="amount">A numeric value of a query's footprint.</param>
        public QueryWeight(int dimensionId, uint amount)
        {
            DimensionId = dimensionId;
            Amount = amount;
        }
    }
}
