using System;

namespace Oakbranch.Binance.Spot
{
    /// <summary>
    /// Represents an order posted from the spot account, either open or historical.
    /// </summary>
    public sealed record SpotOrder : OrderBase
    {
        /// <summary>
        /// Defines the ID of the order list. The value is -1 for non-OCO orders.
        /// </summary>
        public long OrderListId = -1;
        /// <summary>
        /// Defines the type of the order.
        /// </summary>
        public OrderType Type;
        /// <summary>
        /// Defines the time-in-force rule for the order.
        /// </summary>
        public TimeInForce TimeInForce;
        /// <summary>
        /// Defines the quote asset quantity ordered in a post order request.
        /// <para>The value is <see langword="null"/> if this term is not applicable to the order.</para>
        /// </summary>
        public decimal? OriginalQuoteQuantity;
        /// <summary>
        /// Defines the quantity of a single iceberg part of the order.
        /// </summary>
        public decimal? IcebergQuantity;
        /// <summary>
        /// Defines the self-trade prevention mode of the order.
        /// </summary>
        public SelfTradePreventionMode STPMode;
        /// <summary>
        /// Defines the working time of the order.
        /// </summary>
        public DateTime WorkingTime;
        /// <summary>
        /// Defines the ID of the prevented match for the order.
        /// <para>The value is not <see langword="null"/> only if the order has been expired due to the self-trade prevention trigger.</para>
        /// </summary>
        public long? PreventedMatchId;
        /// <summary>
        /// Defines the trade quantity prevented due to the self-trade prevention trigger.
        /// <para>The value is not <c>Null</c> only if the order has been expired due to the self-trade prevention trigger.</para>
        /// </summary>
        public decimal? PreventedQuantity;

        public override string ToString()
        {
            return $"Spot order {OrderId}: {Type}, {Status}";
        }
    }
}
