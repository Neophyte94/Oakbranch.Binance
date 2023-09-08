using System;

namespace Oakbranch.Binance
{
    public abstract class OrderBase
    {
        /// <summary>
        /// Defines the unique ID of a order.
        /// </summary>
        public long OrderId;
        /// <summary>
        /// Defines the custom ID assigned to an order either automatically (by the server) or manually (specified in a post order query).
        /// <para>The manually assigned ID is automatically changed if an order is canceled.</para>
        /// </summary>
        public string ClientOrderId;
        /// <summary>
        /// Defines the trading pair that an order was posted on.
        /// </summary>
        public string Symbol;
        /// <summary>
        /// Defines the side of an order.
        /// </summary>
        public OrderSide Side;
        /// <summary>
        /// Defines the limit order price.
        /// <para>Use the value -1.0m to denote a non-limit order.</para>
        /// </summary>
        public decimal? Price;
        /// <summary>
        /// Defines the base asset quantity ordered in a post order request.
        /// <para>Use the <c>Null</c> value if this term is not applicable to the order.</para>
        /// </summary>
        public decimal? OriginalBaseQuantity;
        /// <summary>
        /// Defines the cummulative base asset quantity of all trades executed within an order so far.
        /// </summary>
        public decimal ExecutedBaseQuantity;
        /// <summary>
        /// Defines the cummulative quote asset quantity of all trades executed within an order so far.
        /// <para>Use the <c>Null</c> value if this term is not applicable to the order.</para>
        /// </summary>
        public decimal? CummulativeQuoteQuantity;
        /// <summary>
        /// Defines the activation price of a stop loss order.
        /// <para>Use the <c>Null</c> value if this term is not applicable to the order.</para>
        /// </summary>
        public decimal? StopPrice;
        /// <summary>
        /// Defines the current order status.
        /// <para>Note that some statuses are only applicable to certain account types.</para>
        /// </summary>
        public OrderStatus Status;
        /// <summary>
        /// Defines the time when an order was first processed (either posted or rejected).
        /// </summary>
        public DateTime? Time;
        /// <summary>
        /// Defines the time when an order's status was updated.
        /// </summary>
        public DateTime? UpdateTime;
        /// <summary>
        /// Indicates whether the order has been activated by the matching engine or not.
        /// <para>The value is <c>True</c> if the order is activated and waiting to be filled, or has already been filled.</para>
        /// <para>The value is <c>False</c> in all the other cases, i.e. waiting for some conditions to be activated, or cancelled, or rejected.</para>
        /// </summary>
        public bool IsWorking;
    }
}
