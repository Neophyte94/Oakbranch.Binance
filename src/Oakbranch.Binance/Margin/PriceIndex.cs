using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Represents a price index for a symbol at a specific calculation time.
    /// </summary>
    public readonly struct PriceIndex
    {
        /// <summary>
        /// Defines the symbol for which the price index is calculated.
        /// </summary>
        public readonly string Symbol;

        /// <summary>
        /// Defines the calculation time of the price index.
        /// </summary>
        public readonly DateTime CalculationTime;

        /// <summary>
        /// Defines the price index value.
        /// </summary>
        public readonly decimal Price;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriceIndex"/> struct.
        /// </summary>
        /// <param name="symbol">The symbol for which the price index is calculated.</param>
        /// <param name="calculationTime">The calculation time of the price index.</param>
        /// <param name="price">The price index value.</param>
        public PriceIndex(string symbol, DateTime calculationTime, decimal price)
        {
            Symbol = symbol;
            CalculationTime = calculationTime;
            Price = price;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="PriceIndex"/> struct.
        /// </summary>
        /// <returns>A string representation of the <see cref="PriceIndex"/> struct.</returns>
        public override string ToString()
        {
            return $"{Symbol} price index: Price = {Price}, Time = {CalculationTime}";
        }
    }
}
