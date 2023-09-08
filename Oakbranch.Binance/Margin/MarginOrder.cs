using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Represents an order posted from a margin account, either open or historical.
    /// </summary>
    public class MarginOrder : OrderBase
    {
        /// <summary>
        /// Defines the type of a margin account the order was posted from.
        /// <para>The value <c>True</c> for an isolated margin account, <c>False</c> for the cross margin account.</para>
        /// </summary>
        public bool IsIsolated;
        /// <summary>
        /// Defines the identifier of the account the order was posted from.
        /// </summary>
        public long? AccountId;
        /// <summary>
        /// Defines the type of the order.
        /// </summary>
        public OrderType Type;
        /// <summary>
        /// Defines the time-in-force rule for the order.
        /// </summary>
        public TimeInForce TimeInForce;
        /// <summary>
        /// Defines the quantity of a single iceberg part of the order.
        /// </summary>
        public decimal? IcebergQuantity;
        /// <summary>
        /// Defines the working time of the order.
        /// </summary>
        public DateTime WorkingTime;

        public override string ToString()
        {
            return $"Spot order {OrderId}: {Type}, {Status}";
        }
    }
}
