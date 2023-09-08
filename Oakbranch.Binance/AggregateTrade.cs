using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Represents a compressed, aggregate trade, with a quantity combined from all trades
    /// that fill at the time, from the same taker order, with the same price.
    /// </summary>
    public readonly struct AggregateTrade
    {
        /// <summary>
        /// Defines the ID of an aggregate trade.
        /// </summary>
        public readonly long Id;
        /// <summary>
        /// Defines the price of an aggregate trade.
        /// </summary>
        public readonly decimal Price;
        /// <summary>
        /// Defines the quantity of an aggregate trade, in the base asset.
        /// </summary>
        public readonly decimal Quantity;
        /// <summary>
        /// Defines the ID of the first trade included in this aggregate trade.
        /// </summary>
        public readonly long FirstTradeId;
        /// <summary>
        /// Defines the ID of the last trade included in this aggregate trade.
        /// </summary>
        public readonly long LastTradeId;
        /// <summary>
        /// Defines the date &amp; time of an aggregate trade (with a granularity of 1 millisecond).
        /// </summary>
        public readonly DateTime Timestamp;
        /// <summary>
        /// Defines whether the buyer was a maker in the trade.
        /// </summary>
        public readonly bool IsBuyerMaker;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateTrade"/> struct.
        /// </summary>
        /// <param name="id">The ID of the aggregate trade.</param>
        /// <param name="price">The price of the aggregate trade.</param>
        /// <param name="quantity">The quantity of the aggregate trade, in the base asset.</param>
        /// <param name="firstTradeId">The ID of the first trade included in this aggregate trade.</param>
        /// <param name="lastTradeId">The ID of the last trade included in this aggregate trade.</param>
        /// <param name="timestamp">The date &amp; time of the aggregate trade (with a granularity of 1 millisecond).</param>
        /// <param name="isBuyerMaker">Whether the buyer was a maker in the trade.</param>
        public AggregateTrade(long id, decimal price, decimal quantity,
            long firstTradeId, long lastTradeId, DateTime timestamp, bool isBuyerMaker)
        {
            Id = id;
            Price = price;
            Quantity = quantity;
            FirstTradeId = firstTradeId;
            LastTradeId = lastTradeId;
            Timestamp = timestamp;
            IsBuyerMaker = isBuyerMaker;
        }


        public override string ToString()
        {
            return $"Aggr trade: ID = {Id}, Time = {Timestamp}, Price = {Price}, Quantity = {Quantity}";
        }
    }
}