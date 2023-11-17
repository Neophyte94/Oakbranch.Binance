using System;

namespace Oakbranch.Binance.Models
{
    /// <summary>
    /// Defines different types of exchange-level filters.
    /// </summary>
    public enum ExchangeFilterType
    {
        /// <summary>
        /// Defines the maximum allowed number of open orders per account.
        /// <para>This limit counts for both normal and "algo" orders.</para>
        /// </summary>
        TotalOpenOrders,
        /// <summary>
        /// Defines the maximum allowed number of open "algo" orders per account.
        /// <para>"Algo" orders are all variations of stop-loss and take-profit orders.</para>
        /// </summary>
        TotalAlgoOrders
    }

    /// <summary>
    /// Defines different types of symbol-level trading filters.
    /// </summary>
    public enum SymbolFilterType
    {
        /// <summary>
        /// Defines the allowed absolute price range for an order on a symbol.
        /// </summary>
        AbsolutePrice,
        /// <summary>
        /// Defines the allowed relative price range for an order on a symbol.
        /// </summary>
        RelativePrice,
        /// <summary>
        /// Defines the allowed relative price range for an order on a symbol, separated by buy and sell sides.
        /// </summary>
        RelativePriceBySide,
        /// <summary>
        /// Defines the allowed quantity range for an order on a symbol.
        /// </summary>
        LotSize,
        /// <summary>
        /// Defines the acceptable notional range allowed for an order on a symbol. 
        /// </summary>
        NotionalRange,
        /// <summary>
        /// Defines the minimum notional value allowed for an order on a symbol.
        /// </summary>
        MinNotional,
        /// <summary>
        /// Defines the allowed maximum number of "iceberg" parts for an order on a symbol.
        /// </summary>
        IcebergParts,
        /// <summary>
        /// Defines the allowed quantity range for a market order on a symbol.
        /// </summary>
        MarketLotSize,
        /// <summary>
        /// Defines the maximum number of orders an account is allowed to have open on a symbol.
        /// <para>This limit counts for both "algo" orders and normal orders.</para>
        /// </summary>
        OpenOrders,
        /// <summary>
        /// Defines the maximum number of "algo" orders an account is allowed to have open on a symbol. 
        /// <para>"Algo" orders are <see cref="OrderType.StopLoss"/>, <see cref="OrderType.StopLossLimit"/>, 
        /// <see cref="OrderType.TakeProfit"/>, and <see cref="OrderType.TakeProfitLimit"/> orders.</para>
        /// </summary>
        AlgoOrders,
        /// <summary>
        /// Defines the maximum number of iceberg orders an account is allowed to have open on a symbol.
        /// <para>An iceberg order is any order where the <see cref="Order.IcebergQuantity"/> is greater than 0.</para>
        /// </summary>
        IcebergOrders,
        /// <summary>
        /// Defines the allowed maximum position an account can have on the base asset of a symbol. 
        /// <para>Buy orders will be rejected if the account's position is greater than the maximum position allowed.</para>
        /// </summary>
        MaxPosition,
        /// <summary>
        /// Defines the minimum and maximum value for the parameter trailingDelta.
        /// </summary>
        TrailingDelta,
    }

    /// <summary>
    /// Defines sides of a trading order.
    /// </summary>
    public enum OrderSide
    {
        Buy,
        Sell
    }

    /// <summary>
    /// Defines possible statuses of a trading order.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// The order has been accepted by the engine.
        /// </summary>
        New,
        /// <summary>
        /// A part of the order has been filled.
        /// </summary>
        PartiallyFilled,
        /// <summary>
        /// The order has been completed.
        /// </summary>
        Filled,
        /// <summary>
        /// The order has been canceled by the user.
        /// </summary>
        Canceled,
        /// <summary>
        /// The order was not accepted by the engine and not processed.
        /// </summary>
        Rejected,
        /// <summary>
        /// The order was canceled according to the order type's rules (see <see cref="TimeInForce"/>) 
        /// or by the exchange (e.g. orders canceled during liquidation, orders canceled during maintenance).
        /// </summary>
        Expired,
        /// <summary>
        /// The order was canceled by the exchange due to the self-trade prevention trigger.
        /// <para>This status is only applicable to the spot trading.</para>
        /// </summary>
        ExpiredInMatch
    }

    public enum TransactionStatus
    {
        /// <summary>
        /// Execution of a transaction is pending.
        /// </summary>
        Pending,
        /// <summary>
        /// Transaction is successfully executed.
        /// </summary>
        Confirmed,
        /// <summary>
        /// Execution of a transaction failed. Nothing happened to an account.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Represents different Binance account types.
    /// </summary>
    public enum AccountType
    {
        Unknown,
        Spot,
        CrossMargin,
        IsolatedMargin,
        UMFutures,
        CMFutures,
        Options,
        Funding
    }
}
