using System;

namespace Oakbranch.Binance.Models.Spot
{
    /// <summary>
    /// Represents a ticker price for a symbol on an exchange.
    /// </summary>
    public readonly struct PriceTick
    {
        /// <summary>
        /// Defines the symbol the price tick represents.
        /// </summary>
        public readonly string Symbol;

        /// <summary>
        /// Defines the price of the symbol.
        /// </summary>
        public readonly decimal Price;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriceTick"/> struct with the specified symbol and price.
        /// </summary>
        /// <param name="symbol">The symbol the price tick represents.</param>
        /// <param name="price">The price of the symbol.</param>
        public PriceTick(string symbol, decimal price)
        {
            Symbol = symbol;
            Price = price;
        }

        public override string ToString()
        {
            return $"{Symbol} price tick: {Price}";
        }
    }
}
