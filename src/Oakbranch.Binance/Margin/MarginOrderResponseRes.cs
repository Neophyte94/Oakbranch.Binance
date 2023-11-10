using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Represents a response of the type <see cref="OrderResponseType.Result"/> to a post order request.
    /// </summary>
    public sealed record MarginOrderResponseRes : MarginOrderResponseBase
    {
        public override OrderResponseType Type => OrderResponseType.Result;
        /// <summary>
        /// Defines the limit order price.
        /// <para>Use the <c>Null</c> value for a non-limit order.</para>
        /// </summary>
        public decimal? Price;
        /// <summary>
        /// Defines the action price of the stop loss / take profit order.
        /// <para>Use the <c>Null</c> value for an order of a different type.</para>
        /// </summary>
        public decimal? StopPrice;
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
        /// Defines the status of the order.
        /// </summary>
        public OrderStatus OrderStatus;
        /// <summary>
        /// Defines the time-in-force rule for the order.
        /// </summary>
        public TimeInForce TimeInForce;
        /// <summary>
        /// Defines the type of the order.
        /// </summary>
        public OrderType OrderType;
        /// <summary>
        /// Defines the side of the order (buy / sell).
        /// </summary>
        public OrderSide OrderSide;
    }
}
