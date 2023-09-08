using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Represents a anonymous trade on an exchange.
    /// </summary>
    public readonly struct Trade
    {
        /// <summary>
        /// Defines the ID of a trade.
        /// </summary>
        public readonly long Id;
        /// <summary>
        /// Defines the price of a trade.
        /// </summary>
        public readonly decimal Price;
        /// <summary>
        /// Defines the base asset quantity of a trade.
        /// </summary>
        public readonly decimal Quantity;
        /// <summary>
        /// Defines the quote asset quantity of a trade.
        /// </summary>
        public readonly decimal QuoteQuantity;
        /// <summary>
        /// Defines the date &amp; time of a trade.
        /// </summary>
        public readonly DateTime Time;
        /// <summary>
        /// Defines whether the buyer was a maker in the trade.
        /// </summary>
        public readonly bool IsBuyerMaker;

        /// <summary>
        /// Creates a new instance of the <see cref="Trade"/> struct.
        /// </summary>
        /// <param name="id">The ID of the trade.</param>
        /// <param name="price">The price of the trade.</param>
        /// <param name="quantity">The base asset quantity of the trade.</param>
        /// <param name="quoteQuantity">The quote asset quantity of the trade.</param>
        /// <param name="time">The date &amp; time of the trade.</param>
        /// <param name="isBuyerMaker">Indicates whether the buyer was a maker in the trade.</param>
        public Trade(long id, decimal price, decimal quantity, decimal quoteQuantity, DateTime time, bool isBuyerMaker)
        {
            Id = id;
            Price = price;
            Quantity = quantity;
            QuoteQuantity = quoteQuantity;
            Time = time;
            IsBuyerMaker = isBuyerMaker;
        }

        public override string ToString()
        {
            return $"Trade: ID={Id}, Time={Time}, Price={Price}, Quantity={Quantity}";
        }
    }
}
