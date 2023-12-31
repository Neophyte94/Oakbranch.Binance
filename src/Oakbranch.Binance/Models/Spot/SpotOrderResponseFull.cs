﻿using System;
using System.Collections.Generic;
using Oakbranch.Binance.Models;

namespace Oakbranch.Binance.Models.Spot
{
    /// <summary>
    /// Represents a response of the type <see cref="OrderResponseType.Full"/> to a post order request.
    /// </summary>
    public sealed record SpotOrderResponseFull : SpotOrderResponseBase
    {
        public override OrderResponseType Type => OrderResponseType.Full;
        /// <summary>
        /// Defines the list of partial fills for the order.
        /// </summary>
        public List<OrderPartialFill>? Fills;
        /// <summary>
        /// Defines the type of the order.
        /// </summary>
        public OrderType OrderType;
        /// <summary>
        /// Defines the side  of the order (buy / sell).
        /// </summary>
        public OrderSide OrderSide;
        /// <summary>
        /// Defines the status of the order.
        /// </summary>
        public OrderStatus OrderStatus;
        /// <summary>
        /// Defines the time-in-force rule for the order.
        /// </summary>
        public TimeInForce TimeInForce;
        /// <summary>
        /// Defines the self-trade prevention mode for the order.
        /// </summary>
        public SelfTradePreventionMode STPMode;
        /// <summary>
        /// Defines the working time of the order.
        /// </summary>
        public DateTime WorkingTime;
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
        /// Defines an amount of either the base or the quote asset borrowed as a side effect of a margin order.
        /// <para>If no borrowing is involved then the value is null.</para>
        /// </summary>
        public double? MarginBuyBorrowAmount;
        /// <summary>
        /// Defines an asset borrowed as a side effect of a margin order.
        /// <para>If no borrowing is involved then the value is null.</para>
        /// </summary>
        public string? MarginBuyBorrowAsset;
        /// <summary>
        /// Defines the ID of the order strategy that the order is part of.
        /// <para>The value is not <c>Null</c> only if the corresponding parameter was specified on order placement.</para>
        /// </summary>
        public int? StrategyId;
        /// <summary>
        /// Defines the type of the order strategy that the order is part of.
        /// <para>The value is not <c>Null</c> only if the corresponding parameter was specified on order placement.</para>
        /// </summary>
        public int? StrategyType;
    }
}
