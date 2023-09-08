using System;

namespace Oakbranch.Binance.Savings
{
    /// <summary>
    /// Represents an earned interest reward for a savings product.
    /// </summary>
    public readonly struct InterestRecord
    {
        /// <summary>
        /// The asset that the interest was earned in.
        /// </summary>
        public readonly string Asset;
        /// <summary>
        /// The quantity of the asset earned.
        /// </summary>
        public readonly decimal Interest;
        /// <summary>
        /// The date &amp; time when the interest was earned.
        /// </summary>
        public readonly DateTime Time;
        /// <summary>
        /// The type of savings productfor which the interest was earned.
        /// </summary>
        public readonly SavingsProductType Type;
        /// <summary>
        /// The name of the savings product for which the interest was earned.
        /// </summary>
        public readonly string ProductName;

        /// <summary>
        /// Creates a new instance of the <see cref="InterestRecord"/> struct.
        /// </summary>
        /// <param name="asset">The asset that the interest was accrued in.</param>
        /// <param name="interest">The quantity of the asset earned.</param>
        /// <param name="time">The date &amp; time when the interest was earned.</param>
        /// <param name="type">The type of savings product for which the interest was earned.</param>
        /// <param name="productName">The name of the savings product for which the interest was earned.</param>
        public InterestRecord(string asset, decimal interest, DateTime time, SavingsProductType type, string productName)
        {
            Asset = asset;
            Interest = interest;
            Time = time;
            Type = type;
            ProductName = productName;
        }
    }
}