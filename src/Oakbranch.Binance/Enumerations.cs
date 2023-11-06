using System;
using Oakbranch.Binance.Exceptions;
using Oakbranch.Binance.RateLimits;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Defines different stages of a web API client's lifespan.
    /// </summary>
    internal enum ClientState
    {
        /// <summary>
        /// The client needs initialization to start working.
        /// </summary>
        Created,
        /// <summary>
        /// The client has been initialized and ready to work.
        /// </summary>
        Running,
        /// <summary>
        /// The client has been disposed.
        /// </summary>
        Disposed
    }

    /// <summary>
    /// Defines different HTTP methods.
    /// </summary>
    public enum HttpMethod
    {
        GET = 0,
        POST = 1,
        PUT = 2,
        DELETE = 3,
    }

    /// <summary>
    /// Defines different types of the API network.
    /// </summary>
    public enum NetworkType
    {
        /// <summary>
        /// The real network.
        /// </summary>
        Live,
        /// <summary>
        /// The test network.
        /// </summary>
        Test
    }

    public enum RateLimitType
    {
        /// <summary>
        /// An IP-based limit for any web requests.
        /// </summary>
        RawRequests = 0,
        /// <summary>
        /// An IP-based limit for API endpoints.
        /// </summary>
        IP = 1,
        /// <summary>
        /// An account-based limit for API endpoints.
        /// </summary>
        UID = 2
    }

    public enum Interval
    {
        Second,
        Minute,
        Hour,
        Day
    }

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

    /// <summary>
    /// Defines possible reasons for a failure of a web query to the Binance server.
    /// </summary>
    public enum FailureReason
    {
        /// <summary>
        /// An error not represented by the other enumeration values.
        /// </summary>
        Other,
        /// <summary>
        /// An HTTP request timed out, the status of the query is unknown.
        /// </summary>
        Timeout,
        /// <summary>
        /// An endpoint could not be reached, the query failed.
        /// <para>This error typically occurs when there is no Internet connection.</para>
        /// </summary>
        ConnectionFailed,
        /// <summary>
        /// A sender is not authorized to execute a request, the query failed.
        /// </summary>
        Unauthorized,
        /// <summary>
        /// One or more query parameters have invalid values or are missing.
        /// <para>More information about the error is typically provided in <see cref="QueryInputException"/>.</para>
        /// </summary>
        InvalidInput,
        /// <summary>
        /// A query has been rejected by the Binance server due to being receiving outside its time window.
        /// </summary>
        RequestOutdated,
        /// <summary>
        /// The WAF Limit (Web Application Firewall) has been violated.
        /// </summary>
        WAFLimitViolated,
        /// <summary>
        /// A query has not been sent due to a risk of violating one of the API rate limits.
        /// </summary>
        RateLimitPrevention,
        /// <summary>
        /// One of the API rate limits has been violated.
        /// <para>This error may be a sign of a bad implementation of <see cref="IRateLimitsRegistry"/> or the class using it.</para>
        /// </summary>
        RateLimitViolated,
        /// <summary>
        /// A query has not been sent due to the automatic delay after the <see cref="RateLimitViolated"/> error.
        /// <para>During the delay, new queries are automatically blocked to prevent an IP to be banned by the Binance server.</para>
        /// </summary>
        BanPreventionBlock,
        /// <summary>
        /// An IP has been auto-banned for continuing to send requests after the <see cref="RateLimitViolated"/> error.
        /// <para>This error may be a sign of a bad implementation of <see cref="IRateLimitsRegistry"/> or the class using it.</para>
        /// </summary>
        IPAutoBanned,
        /// <summary>
        /// An issue occurred on Binance's side. 
        /// <para>It is important to NOT treat this as a failure operation; the execution status is unknown and could have been a success.</para>
        /// </summary>
        BinanceInternalError,
        /// <summary>
        /// A response received from the Binance server could not be parsed.
        /// </summary>
        UnknownResponseFormat
    }

    /// <summary>
    /// Defines possible errors of input data in a web query to the Binance server.
    /// </summary>
    public enum InputErrorCode
    {
        /// <summary>
        /// The error is not represented by any of the other values.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// An unsupported order combination.
        /// </summary>
        InvalidCombination = -1014,
        /// <summary>
        /// Too many new orders.
        /// </summary>
        TooManyOrders = -1015,
        /// <summary>
        /// This service is no longer available.
        /// </summary>
        ServiceUnavailable = -1016,
        /// <summary>
        /// This operation is not supported.
        /// </summary>
        OperationNotSupported = -1020,
        /// <summary>
        /// A timestamp provided for a request was outside of the recieve window, or it was 1000ms ahead of the server's time.
        /// </summary>
        InvalidTimestamp = -1021,
        /// <summary>
        /// A signature for a request was not valid.
        /// </summary>
        InvalidSignature = -1022,
        /// <summary>
        /// Illegal characters found in a parameter.
        /// </summary>
        IllegalCharacters = -1100,
        /// <summary>
        /// Too many parameters sent for an endpoint, or duplicate values for a parameter detected.
        /// </summary>
        TooManyParameters = -1101,
        /// <summary>
        /// A mandatory parameter was not sent, was empty/null, or malformed.
        /// </summary>
        ParameterMissing = -1102,
        /// <summary>
        /// An unknown parameter was sent.
        /// </summary>
        UnknownParameter = -1103,
        /// <summary>
        /// Not all sent parameters were read.
        /// </summary>
        NotAllParametersRead = -1104,
        /// <summary>
        /// One or more parameters sent were empty.
        /// </summary>
        EmptyParameter = -1105,
        /// <summary>
        /// A parameter was sent when not required.
        /// </summary>
        ParameterNotRequired = -1106,
        /// <summary>
        /// A precision of the provided value was over the maximum defined for the asset.
        /// </summary>
        PrecisionOverMaximum = -1111,
        /// <summary>
        /// No orders on book for symbol.
        /// </summary>
        NoOrdersOnBook = -1112,
        /// <summary>
        /// The time-in-force parameter sent when not required.
        /// </summary>
        TimeInForceParameterNotRequired = -1114,
        /// <summary>
        /// An invalid value of the time-in-force parameter was provided.
        /// </summary>
        InvalidTimeInForce = -1115,
        /// <summary>
        /// An invalid value of the order type parameter was provided.
        /// </summary>
        InvalidOrderType = -1116,
        /// <summary>
        /// An invalid value of the order side parameter was provided.
        /// </summary>
        InvalidSide = -1117,
        /// <summary>
        /// A provided value of the new client order ID parameter was empty.
        /// </summary>
        NewClientOrderIdEmpty = -1118,
        /// <summary>
        /// A provided value of the original client order ID parameter was empty.
        /// </summary>
        OriginalClientOrderIdEmpty = -1119,
        /// <summary>
        /// An invalid value of the interval parameter was provided.
        /// </summary>
        InvalidInterval = -1120,
        /// <summary>
        /// An invalid value of the symbol parameter was provided.
        /// </summary>
        InvalidSymbol = -1121,
        /// <summary>
        /// The specified listen key does not exist.
        /// </summary>
        ListenKeyNotFound = -1125,
        /// <summary>
        /// The provided lookup interval was too big.
        /// </summary>
        LookupIntervalTooBig = -1127,
        /// <summary>
        /// A provided combination of optional parameters was invalid.
        /// </summary>
        InvalidCombinationOfOptionalParameters = -1128,
        /// <summary>
        /// Invalid data was sent for a parameter.
        /// </summary>
        InvalidDataForParameter = -1130,
        /// <summary>
        /// A provided value of the request window parameter was too large.
        /// </summary>
        RequestWindowTooLarge = -1131,
        /// <summary>
        /// A provided value of the strategy type parameter was less than 1000000.
        /// </summary>
        BadStrategyType = -1134,
        /// <summary>
        /// An provided value of the cancellation restrictions parameter was invalid.
        /// </summary>
        InvalidCancelRestrictions = -1145,
        /// <summary>
        /// A new order was rejected.
        /// </summary>
        NewOrderRejected = -2010,
        /// <summary>
        /// An order's cancellation was rejected.
        /// </summary>
        CancelOrderRejected = -2011,
        /// <summary>
        /// A requested order does not exist.
        /// </summary>
        OrderNotExists = -2013,
        /// <summary>
        /// An API-key format was invalid.
        /// </summary>
        BadApiKey = -2014,
        /// <summary>
        /// An invalid API-key, IP, or permissions for an action.
        /// </summary>
        RejectedMbxKey = -2015,
        /// <summary>
        /// No trading window could be found for the symbol. 
        /// </summary>
        NoTradingWindow = -2016,
        /// <summary>
        /// An order was canceled or expired with no executed quantity over 90 days ago, and has been archived.
        /// </summary>
        OrderArchived = -2026,
        /// <summary>
        /// The enabled two-factor-authorization is required for the operation.
        /// </summary>
        NeedEnable2FA = -3001,
        /// <summary>
        /// The insufficient system availability of the asset.
        /// </summary>
        AssetDeficiency = -3002,
        /// <summary>
        /// A margin account does not exist.
        /// </summary>
        NoOpenedMarginAccount = -3003,
        /// <summary>
        /// A trade is not allowed.
        /// </summary>
        TradeNotAllowed = -3004,
        /// <summary>
        /// Transferring out is not allowed.
        /// </summary>
        TransferOutNotAllowed = -3005,
        /// <summary>
        /// A user's borrow amount has exceed his maximum borrow amount.
        /// </summary>
        ExceedMaxBorrowable = -3006,
        /// <summary>
        /// A user has a pending transaction. The operation should be retried later.
        /// </summary>
        HasPendingTransaction = -3007,
        /// <summary>
        /// Borrowing is not allowed.
        /// </summary>
        BorrowNotAllowed = -3008,
        /// <summary>
        /// An asset is currently not available for transfering into a margin account.
        /// </summary>
        AssetNotMortgageable = -3009,
        /// <summary>
        /// Repayment is not allowed.
        /// </summary>
        RepayNotAllowed = -3010,
        /// <summary>
        /// A provided date value was invalid.
        /// </summary>
        BadDateRange = -3011,
        /// <summary>
        /// Borrowing was banned for the asset.
        /// </summary>
        AssetAdminBanBorrow = -3012,
        /// <summary>
        /// A borrow amount was less than the minimum borrow amount.
        /// </summary>
        LtMinBorrowable = -3013,
        /// <summary>
        /// Borrowing was banned for the account.
        /// </summary>
        AccountBanBorrow = -3014,
        /// <summary>
        /// A repayment amount exceeded the borrow amount.
        /// </summary>
        RepayExceedLiability = -3015,
        /// <summary>
        /// A repayment amount was less than the minimum repayment amount.
        /// </summary>
        LtMinRepay = -3016,
        /// <summary>
        /// The asset is currently not allowed for transfering into a margin account.
        /// </summary>
        AssetAdminBanMortgage = -3017,
        /// <summary>
        /// Transferring in has been banned for the account.
        /// </summary>
        AccountBanMortgage = -3018,
        /// <summary>
        /// Transferring out has been banned for this account.
        /// </summary>
        AccountBanRollout = -3019,
        /// <summary>
        /// A transfer out amount exceeds max amount.
        /// </summary>
        ExceedMaxRollout = -3020,
        /// <summary>
        /// The margin account was not allowed to trade the trading pair.
        /// </summary>
        PairAdminBanTrade = -3021,
        /// <summary>
        /// Trading was banned for the account.
        /// </summary>
        AccountBanTrade = -3022,
        /// <summary>
        /// A requested operation is forbidden under the current margin level.
        /// </summary>
        WarningMarginLevel = -3023,
        /// <summary>
        /// An unpaid debt would be too small after a requested repayment.
        /// </summary>
        FewLiabilityLeft = -3024,
        /// <summary>
        /// A provided date value was invalid.
        /// </summary>
        InvalidEffectiveTime = -3025,
        /// <summary>
        /// One of the input parameters was invalid.
        /// </summary>
        ValidationFailed = -3026,
        /// <summary>
        /// A provided value of the margin asset parameter was invalid.
        /// </summary>
        NotValidMarginAsset = -3027,
        /// <summary>
        /// A provided value of the margin pair parameter was invalid.
        /// </summary>
        NotValidMarginPair = -3028,
        /// <summary>
        /// A transfer failed.
        /// </summary>
        TransferFailed = -3029,
        /// <summary>
        /// An account was not allowed to repay.
        /// </summary>
        AccountBanRepay = -3036,
        /// <summary>
        /// PNL is clearing. It is recommended to wait a second.
        /// </summary>
        PnlClearing = -3037,
        /// <summary>
        /// A listen key was not found.
        /// </summary>
        SapiListenKeyNotFound = -3038,
        /// <summary>
        /// A balance was not enough.
        /// </summary>
        BalanceNotEnough = -3041,
        /// <summary>
        /// A price index is not available for the margin pair.
        /// </summary>
        PriceIndexNotFound = -3042,
        /// <summary>
        /// Transferring was not allowed.
        /// </summary>
        TransferInNotAllowed = -3043,
        /// <summary>
        /// The system is busy.
        /// </summary>
        SystemBusy = -3044,
        /// <summary>
        /// The insufficient system availability of the asset.
        /// </summary>
        InsufficientAsset = -3045,
    }
}
