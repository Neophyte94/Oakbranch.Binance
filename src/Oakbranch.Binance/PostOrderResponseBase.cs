using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Represents a base for a response to a post order request.
    /// </summary>
    public abstract class PostOrderResponseBase
    {
        /// <summary>
        /// Defines the symbol that the order was placed on.
        /// </summary>
        public string Symbol;
        /// <summary>
        /// Defines the unique ID of the order.
        /// </summary>
        public long OrderId;
        /// <summary>
        /// Defines the custom ID of the order.
        /// <para>The custom ID can be assigned either in the post order request or automatically by the server.</para>
        /// <para>A cancelled order can get its custom ID automatically changed by the server.</para>
        /// </summary>
        public string ClientOrderId;
        /// <summary>
        /// Defines the time when the order was processed (either posted or rejected).
        /// </summary>
        public DateTime TransactionTime;
    }
}