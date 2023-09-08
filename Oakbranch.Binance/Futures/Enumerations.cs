using System;

namespace Oakbranch.Binance.Futures
{
    /// <summary>
    /// Defines different types of futures contracts.
    /// </summary>
    public enum ContractType
    {
        /// <summary>
        /// A contract that has no expiration date and does not deliver an underlying asset.
        /// </summary>
        Perpetual,
        /// <summary>
        /// A contract that expires at the end of the current month (USD-margined futures only).
        /// </summary>
        CurrentMonth,
        /// <summary>
        /// A contract that expires at the end of the next month (USD-margined futures only).
        /// </summary>
        NextMonth,
        /// <summary>
        /// A contract that expires at the end of the current quarter.
        /// </summary>
        CurrentQuarter,
        /// <summary>
        /// A contract that expires at the end of the next quarter.
        /// </summary>
        NextQuarter,
        /// <summary>
        /// An unknown type.
        /// </summary>
        PerpetualDelivering
    }

    /// <summary>
    /// Defines different statuses of futures contracts.
    /// </summary>
    public enum ContractStatus
    {
        PendingTrading,
        Trading,
        PreDelivering,
        Delivering,
        Delivered,
        PreSettle,
        Settling,
        Close
    }

    /// <summary>
    /// Defines supported types of a futures trading order.</para>
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// An order is only executed at a determined or better price.
        /// <para>A limit order may be immediately filled by taking existing orders of the opposite side from the orders book,
        /// if such orders exist at the moment of posting the order. Otherwise the order is put in the orders book.</para>
        /// </summary>
        Limit,
        /// <summary>
        /// An order is executed at a market price, i.e. by taking existing orders of the opposite side from the order book.
        /// <para>A market order is always filled immediately.</para>
        /// </summary>
        Market,
        /// <summary>
        /// An order is executed at a determined or better price once the price reaches a certain level (named the "stop-loss price").
        /// <para>The stop-loss price must be lower (higher) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        StopLossLimit,
        /// <summary>
        /// An order is executed at a market price once the price reaches a certain level (named the "stop-loss price").
        /// <para>The stop-loss price must be lower (higher) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        StopLossMarket,
        /// <summary>
        /// An order is executed at a determined price once the price reaches a certain level (named the "take-profit price").
        /// <para>The take-profit price must be higher (lower) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        TakeProfitLimit,
        /// <summary>
        /// An order is executed at a market price once the price reaches a certain level (named the "take-profit price").
        /// <para>The take-profit price must be higher (lower) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        TakeProfitMarket,
        /// <summary>
        /// An order starts tracking the price once it reaches a certain level (named the "activation price"),
        /// and it is executed at the market price only when the tracked price breaks out of a certain tolerance range
        /// (determined by the "trailing delta" parameter).
        /// </summary>
        TrailingStopMarket,
    }

    /// <summary>
    /// Defines different sides of a trading position.
    /// </summary>
    public enum PositionSide
    {
        Both,
        Long,
        Short
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
        FillOrKill,
        /// <summary>
        /// An order will either be posted on the orders book or immediately expire.
        /// </summary>
        GoodTillCrossing
    }

    /// <summary>
    /// Defines different working types of stop-loss / take-profit orders.
    /// </summary>
    public enum WorkingType
    {
        MarkPrice,
        ContractPrice
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
        Result
    }

    /// <summary>
    /// Defines candlestick intervals supported by the Binance API.
    /// </summary>
    public enum KlineInterval
    {
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

    /// <summary>
    /// Defines intervals of market stats supported by the Binance API.
    /// </summary>
    public enum StatsInterval
    {
        Minute5,
        Minute15,
        Minute30,
        Hour1,
        Hour2,
        Hour4,
        Hour6,
        Hour12,
        Day1,
    }
}
