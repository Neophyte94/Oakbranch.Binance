using System;
using Oakbranch.Binance.Models;

namespace Oakbranch.Binance.Models.Spot
{
    public abstract record SpotOrderResponseBase : PostOrderResponseBase
    {
        /// <summary>
        /// Gets the type of the response.
        /// </summary>
        public abstract OrderResponseType Type { get; }
        /// <summary>
        /// Defines the ID of the order list. Its value must be -1 for non-OCO orders.
        /// </summary>
        public long OrderListId = -1;
    }
}
