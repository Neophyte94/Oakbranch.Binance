using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Defines different statuses of the API server.
    /// </summary>
    public enum SystemStatus
    {
        /// <summary>
        /// The API server is working.
        /// </summary>
        Normal,
        /// <summary>
        /// The API server is under maintenance.
        /// </summary>
        Maintenance
    }

    /// <summary>
    /// Defines different formal statuses of an account's margin.
    /// </summary>
    public enum MarginStatus
    {
        Excessive,
        Normal,
        MarginCall,
        PreLiquidation,
        ForceLiquidation
    }

    /// <summary>
    /// Defines different side effects of posting or executing a margin order.
    /// </summary>
    public enum MarginSideEffect
    {
        /// <summary>
        /// No side effects associated with an order execution.
        /// </summary>
        NoSideEffect,
        /// <summary>
        /// Auto borrowing if there are insufficient funds for an order.
        /// </summary>
        MarginBuy,
        /// <summary>
        /// Auto repayment of a debt with assets unlocked after an order execution.
        /// </summary>
        AutoRepay
    }

    /// <summary>
    /// Describes asset transfer directions.
    /// </summary>
    public enum TransferDirection
    {
        /// <summary>
        /// An incoming transfer.
        /// </summary>
        RollIn,
        /// <summary>
        /// An outcoming transfer.
        /// </summary>
        RollOut
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
        /// An order is executed at a market price, i.e. by taking existing orders of the opposite side from the order book.
        /// <para>A market order is always filled immediately.</para>
        /// </summary>
        Market,
        /// <summary>
        /// An order is executed at a market price once the price reaches a certain level (named a "stop-loss price").
        /// <para>The stop-loss price must be lower (higher) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        StopLoss,
        /// <summary>
        /// An order is executed at a determined or better price once the price reaches a certain level (named a "stop-loss price").
        /// <para>The stop-loss price must be lower (higher) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        StopLossLimit,
        /// <summary>
        /// An order is executed at a market price once the price reaches a certain level (named a "take-profit price").
        /// <para>The take-profit price must be higher (lower) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        TakeProfit,
        /// <summary>
        /// An order is executed at a determined price once the price reaches a certain level (named a "take-profit price").
        /// <para>The take-profit price must be higher (lower) than a price at the moment of posting a sell (buy) order.</para>
        /// </summary>
        TakeProfitLimit,
        /// <summary>
        /// An order is only executed as a market maker order at a determined or better price. 
        /// <para>A market maker order is rejected if it would immediately match and trade as a taker at the moment of posting it.</para>
        /// </summary>
        LimitMaker
    }

    /// <summary>
    /// Describes how long an order will be active before expiration. 
    /// </summary>
    public enum TimeInForce
    {
        /// <summary>
        /// An order will be on the book unless the order is canceled.
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
