using System;

namespace Oakbranch.Binance.Spot
{
    /// <summary>
    /// Defines candlestick intervals supported by the Binance API.
    /// </summary>
    public enum KlineInterval
    {
        Second1,
        Minute1,
        Minute3,
        Minute5,
        Minute15,
        Minute30,
        Hour1,
        Hour2,
        Hour4,
        Hour6,
        Hour8,
        Hour12,
        Day1,
        Day3,
        Week1,
        Month1
    }

    public enum SymbolStatus
    {
        PreTrading,
        Trading,
        PostTrading,
        EndOfDay,
        Halt,
        AuctionMatch,
        Break
    }

    /// <summary>
    /// Defines different account-level permissions.
    /// <para>This enumeration has the <see cref="FlagsAttribute"/> attribute.</para>
    /// </summary>
    [Flags]
    public enum AccountPermissions
    {
        None = 0,
        Spot = 1,
        Margin = 2,
        Leveraged = 4,
        TRD_GRP_002 = 8,
        TRD_GRP_003 = 16,
        TRD_GRP_004 = 32,
        TRD_GRP_005 = 64,
        TRD_GRP_006 = 128,
        TRD_GRP_007 = 256
    }

    /// <summary>
    /// Defines different symbol-level permissions.
    /// <para>This enumeration has the <see cref="FlagsAttribute"/> attribute.</para>
    /// </summary>
    [Flags]
    public enum SymbolPermissions
    {
        /// <summary>
        /// No permissions set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Trading in the spot account.
        /// </summary>
        SpotTrading = 1,
        /// <summary>
        /// Trading in a margin account, either cross or isolated.
        /// </summary>
        MarginTrading = 2,
        /// <summary>
        /// Trading via iceberg orders.
        /// </summary>
        IcebergOrders = 4,
        /// <summary>
        /// Trading via "One-cancels-other" orders.
        /// </summary>
        OCOOrders = 8,
        /// <summary>
        /// Trading via "market" orders based on quantity of a quote asset.
        /// <para>A "market" order fills with best currently available bids in the order book.</para>
        /// </summary>
        QuoteQuantityOrders = 16,
        /// <summary>
        /// Trading via orders with a trailing stop.
        /// </summary>
        TrailingStopOrders = 32,
        /// <summary>
        /// Cancelling an existing order and placing a new order on the same symbol as an atomic operation.
        /// </summary>
        OrderReplacing,
    }

    /// <summary>
    /// Represents different modes of preventing self-trading.
    /// </summary>
    public enum SelfTradePreventionMode
    {
        None,
        ExpireMaker,
        ExpireTaker,
        ExpireBoth
    }

    /// <summary>
    /// Defines supported types of a trading order.
    /// <para>For more information on the order types definitions visit: 
    /// <see href="https://www.binance.com/en/support/articles/360033779452-Types-of-Order"/></para>
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// An order is only executed at a determined or better price.
        /// <para>A limit order may be immediately filled by taking existing orders of the opposite side 
        /// (at the specified or better price) from the orders book, if such orders exist at the moment of posting the order.
        /// Otherwise the order is itself put in the orders book.</para>
        /// </summary>
        Limit,
        /// <summary>
        /// An order is only executed as a market maker order at a determined or better price. 
        /// <para>A market maker order is rejected if it would immediately match and trade as a taker at the moment of posting it.</para>
        /// </summary>
        LimitMaker,
        /// <summary>
        /// An order is executed at a market price, i.e. by taking existing orders of the opposite side from the order book.
        /// <para>A market order is always filled immediately.</para>
        /// </summary>
        Market,
        /// <summary>
        /// An order is executed at a market price once the price reaches a certain level (named a "stop-loss price").
        /// <para>The stop-loss price must be lower (higher) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        StopLossMarket,
        /// <summary>
        /// An order is executed at a determined or better price once the price reaches a certain level (named a "stop-loss price").
        /// <para>The stop-loss price must be lower (higher) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        StopLossLimit,
        /// <summary>
        /// An order is executed at a market price once the price reaches a certain level (named a "take-profit price").
        /// <para>The take-profit price must be higher (lower) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        TakeProfitMarket,
        /// <summary>
        /// An order is executed at a determined price once the price reaches a certain level (named a "take-profit price").
        /// <para>The take-profit price must be higher (lower) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        TakeProfitLimit,
    }

    /// <summary>
    /// Defines possible statuses of a "one-cancels-other" order list.
    /// </summary>
    public enum OCOStatus
    {
        /// <summary>
        /// This is used when the ListStatus is responding to a failed action. (E.g. Orderlist placement or cancellation)
        /// </summary>
        Response,
        /// <summary>
        /// The order list has been placed or there is an update to the order list status.
        /// </summary>
        ExecutionStarted,
        /// <summary>
        /// The order list has finished executing and thus no longer active.
        /// </summary>
        AllDone
    }

    /// <summary>
    /// Defines possible statuses of a "one-cancels-other" order.
    /// </summary>
    public enum OCOOrderStatus
    {
        /// <summary>
        /// Either an order list has been placed or there is an update to the status of the list.
        /// </summary>
        Executing,
        /// <summary>
        /// An order list has completed execution and thus no longer active.
        /// </summary>
        AllDone,
        /// <summary>
        /// The List Status is responding to a failed action either during order placement or order canceled
        /// </summary>
        Reject
    }

    /// <summary>
    /// Describes how long an order will be active before expiration. 
    /// </summary>
    public enum TimeInForce
    {
        /// <summary>
        /// An order will last until it is completed or canceled.
        /// </summary>
        GoodTillCanceled,
        /// <summary>
        /// An order will try to fill the order as much as it can before the order expires.
        /// </summary>
        ImmediateOrCancel,
        /// <summary>
        /// An order will expire if the full order cannot be filled upon execution.
        /// </summary>
        FillOrKill
    }

    /// <summary>
    /// Defines different rules for order cancellation.
    /// </summary>
    public enum CancellationRestriction
    {
        /// <summary>
        /// Cancel will succeed if the order status is <see cref="OrderStatus.New"/>.
        /// </summary>
        OnlyNew,
        /// <summary>
        /// Cancel will succeed if order status is <see cref="OrderStatus.PartiallyFilled"/>.
        /// </summary>
        OnlyPartiallyFilled
    }

    /// <summary>
    /// Defines possible formats of a response to an order post request.
    /// </summary>
    public enum OrderResponseType
    {
        /// <summary>
        /// The short response format with the order posting acknowledgement.
        /// </summary>
        Ack,
        /// <summary>
        /// The optimized response format containing the summary on the order posting.
        /// </summary>
        Result,
        /// <summary>
        /// The full response format containing both the summary on the order posting and the individual trades list (if any).
        /// </summary>
        Full
    }
}