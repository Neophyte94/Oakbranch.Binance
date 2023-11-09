using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Oakbranch.Binance.RateLimits;
using Oakbranch.Common.Logging;

namespace Oakbranch.Binance.Spot;

public class SpotAccountApiClient : ApiV3ClientBase
{
    #region Constants

    // Endpoints.
    private const string GetAccountInfoEndpoint = "/api/v3/account";
    private const string GetAccountTradeListEndpoint = "/api/v3/myTrades";
    private const string PostTestOrderEndpoint = "/api/v3/order/test";
    private const string PostNewOrderEndpoint = "/api/v3/order";
    private const string GetOrderEndpoint = "/api/v3/order";
    private const string GetAllOpenOrdersEndpoint = "/api/v3/openOrders";
    private const string GetAllOrdersEndpoint = "/api/v3/allOrders";
    private const string DeleteOrderEndpoint = "/api/v3/order";
    private const string DeleteAllOpenOrdersEndpoint = "/api/v3/openOrders";
    private const string PostReplaceOrderEndpoint = "/api/v3/order/cancelReplace";
    private const string PostNewOCOEndpoint = "/api/v3/order/oco";
    private const string GetOCOEndpoint = "/api/v3/orderList";
    private const string GetAllOpenOCOEndpoint = "/api/v3/openOrderList";
    private const string GetAllOCOEndpoint = "/api/v3/allOrderList";
    private const string DeleteOCOEndpoint = "/api/v3/orderList";
    private const string GetOrderLimitUsageEndpoint = "/api/v3/rateLimit/order";
    private const string GetPreventedMatchesEndpoint = "/api/v3/myPreventedMatches";

    // Parameters restrictions.
    /// <summary>
    /// Defines the maximum time range of the account trades query (in ticks).
    /// </summary>
    public const long AccountTradesMaxTimeRange = 24 * TimeSpan.TicksPerHour;

    #endregion

    #region Instance members

    protected override string LogContextName => "Binance SA API client";

    #endregion

    #region Instance constructors

    public SpotAccountApiClient(IApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger logger) :
        base(connector, limitsRegistry, logger)
    {

    }

    #endregion

    #region Static methods

    private static string Format(TimeInForce value)
    {
        switch (value)
        {
            case TimeInForce.GoodTillCanceled:
                return "GTC";
            case TimeInForce.FillOrKill:
                return "FOK";
            case TimeInForce.ImmediateOrCancel:
                return "IOC";
            default:
                throw new NotImplementedException($"The time-in-force type \"{value}\" is not implemented.");
        }
    }

    private static TimeInForce ParseTimeInForce(string s)
    {
        if (String.IsNullOrWhiteSpace(s))
            throw new JsonException($"The time in force rule value is null.");

        switch (s)
        {
            case "GTC":
                return TimeInForce.GoodTillCanceled;
            case "IOC":
                return TimeInForce.ImmediateOrCancel;
            case "FOK":
                return TimeInForce.FillOrKill;
            default:
                throw new JsonException($"An unknown time in force rule \"{s}\" was encountered.");
        }
    }

    private static string Format(OrderType value)
    {
        switch (value)
        {
            case OrderType.Limit:
                return "LIMIT";
            case OrderType.LimitMaker:
                return "LIMIT_MAKER";
            case OrderType.Market:
                return "MARKET";
            case OrderType.StopLossMarket:
                return "STOP_LOSS";
            case OrderType.StopLossLimit:
                return "STOP_LOSS_LIMIT";
            case OrderType.TakeProfitMarket:
                return "TAKE_PROFIT";
            case OrderType.TakeProfitLimit:
                return "TAKE_PROFIT_LIMIT";
            default:
                throw new NotImplementedException($"The order type \"{value}\" is not implemented.");
        }
    }

    private static OrderType ParseOrderType(string s)
    {
        if (String.IsNullOrWhiteSpace(s))
            throw new JsonException($"The order type value is null.");

        switch (s)
        {
            case "LIMIT":
                return OrderType.Limit;
            case "LIMIT_MAKER":
                return OrderType.LimitMaker;
            case "MARKET":
                return OrderType.Market;
            case "STOP_LOSS":
                return OrderType.StopLossMarket;
            case "STOP_LOSS_LIMIT":
                return OrderType.StopLossLimit;
            case "TAKE_PROFIT":
                return OrderType.TakeProfitMarket;
            case "TAKE_PROFIT_LIMIT":
                return OrderType.TakeProfitLimit;
            default:
                throw new JsonException($"The order type \"{s}\" is unknown.");
        }
    }

    private static string Format(CancellationRestriction value)
    {
        switch (value)
        {
            case CancellationRestriction.OnlyNew:
                return "ONLY_NEW";
            case CancellationRestriction.OnlyPartiallyFilled:
                return "ONLY_PARTIALLY_FILLED";
            default:
                throw new NotImplementedException($"The cancellation restriction rule \"{value}\" is not implemented.");
        }
    }

    private static string Format(OrderResponseType value)
    {
        switch (value)
        {
            case OrderResponseType.Ack:
                return "ACK";
            case OrderResponseType.Full:
                return "FULL";
            case OrderResponseType.Result:
                return "RESULT";
            default:
                throw new NotImplementedException($"The order response type \"{value}\" is not implemented.");
        }
    }

    #endregion

    #region Instance methods

    // Account information.
    /// <summary>
    /// Creates a deferred query for the current info on the spot account.
    /// </summary>
    public IDeferredQuery<SpotAccountInfo> PrepareGetAccountInfo()
    {
        ThrowIfNotRunning();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 10),
        };

        return new DeferredQuery<SpotAccountInfo>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAccountInfoEndpoint, null, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseAccountInfo,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets the current info on the spot account.
    /// </summary>
    public Task<SpotAccountInfo> GetAccountInfo(CancellationToken ct)
    {
        using (IDeferredQuery<SpotAccountInfo> query = PrepareGetAccountInfo())
        {
            return query.ExecuteAsync(ct);
        }
    }

    private SpotAccountInfo ParseAccountInfo(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);
        SpotAccountInfo rsp = new SpotAccountInfo();
        decimal? mkCommRate = null, tkCommRate = null, brCommRate = null, slCommRate = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
            {
                throw ParseUtility.GenerateNoPropertyValueException(propName);
            }

            switch (propName)
            {
                case "makerCommission":
                    mkCommRate ??= 0.0001m * reader.GetInt32();
                    break;
                case "takerCommission":
                    tkCommRate ??= 0.0001m * reader.GetInt32();
                    break;
                case "buyerCommission":
                    brCommRate ??= 0.0001m * reader.GetInt32();
                    break;
                case "sellerCommission":
                    slCommRate ??= 0.0001m * reader.GetInt32();
                    break;
                case "commissionRates":
                    ParseUtility.EnsureObjectStartToken(ref reader);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        string commName = ParseUtility.GetNonEmptyPropertyName(ref reader);
                        decimal dcTemp;

                        if (!reader.Read())
                        {
                            throw ParseUtility.GenerateNoPropertyValueException(commName);
                        }

                        switch (commName)
                        {
                            case "maker":
                                ParseUtility.ParseDecimal(commName, reader.GetString(), out dcTemp);
                                mkCommRate = dcTemp;
                                break;
                            case "taker":
                                ParseUtility.ParseDecimal(commName, reader.GetString(), out dcTemp);
                                tkCommRate = dcTemp;
                                break;
                            case "buyer":
                                ParseUtility.ParseDecimal(commName, reader.GetString(), out dcTemp);
                                brCommRate = dcTemp;
                                break;
                            case "seller":
                                ParseUtility.ParseDecimal(commName, reader.GetString(), out dcTemp);
                                slCommRate = dcTemp;
                                break;
                        }
                    }
                    break;
                case "canTrade":
                    rsp.CanTrade = reader.GetBoolean();
                    break;
                case "canWithdraw":
                    rsp.CanWithdraw = reader.GetBoolean();
                    break;
                case "canDeposit":
                    rsp.CanDeposit = reader.GetBoolean();
                    break;
                case "brokered":
                    rsp.IsBrokered = reader.GetBoolean();
                    break;
                case "requireSelfTradePrevention":
                    rsp.RequiresSelfTradePrevention = reader.GetBoolean();
                    break;
                case "updateTime":
                    rsp.UpdateTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    break;
                case "accountType":
                    rsp.AccountType = reader.GetString();
                    break;
                case "balances":
                    rsp.Balances = ParseAccountAssets(ref reader);
                    break;
                case "permissions":
                    rsp.Permissions = ParseAccountPermissions(ref reader);
                    break;
                default:
                    PostLogMessage(LogLevel.Warning, $"An unknown account info property \"{propName}\" was encountered.");
                    reader.Skip();
                    break;
            }
        }

        if (mkCommRate == null || tkCommRate == null)
            throw ParseUtility.GenerateMissingPropertyException("account info", "maker / taker commision rates");
        if (brCommRate == null || slCommRate == null)
            throw ParseUtility.GenerateMissingPropertyException("account info", "buyer / seller commission rates.");
        rsp.MakerCommissionRate = mkCommRate.Value;
        rsp.TakerCommissionRate = tkCommRate.Value;
        rsp.BuyerCommissionRate = brCommRate.Value;
        rsp.SellerCommissionRate = slCommRate.Value;

        return rsp;
    }

    private List<string> ParseAccountPermissions(ref Utf8JsonReader reader)
    {
        ParseUtility.EnsureArrayStartToken(ref reader);
        List<string> resultsList = new List<string>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            string? permission = reader.GetString();
            if (!String.IsNullOrEmpty(permission))
            {
                resultsList.Add(permission);
            }
        }

        return resultsList;
    }

    private List<SpotAsset> ParseAccountAssets(ref Utf8JsonReader reader)
    {
        ParseUtility.EnsureArrayStartToken(ref reader);
        List<SpotAsset> resultsList = new List<SpotAsset>(20);

        string? asset = null;
        decimal free = 0.0m, locked = 0.0m;
        ParseSchemaValidator validator = new ParseSchemaValidator(3);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ParseUtility.EnsureObjectStartToken(ref reader);

            
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                
                // Parse account asset properties.
                switch (propName)
                {
                    case "asset":
                        asset = reader.GetString();
                        validator.RegisterProperty(0);
                        break;
                    case "free":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out free);
                        validator.RegisterProperty(1);
                        break;
                    case "locked":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out locked);
                        validator.RegisterProperty(2);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "spot asset";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => ParseUtility.GenerateMissingPropertyException(objName, "asset"),
                    1 => ParseUtility.GenerateMissingPropertyException(objName, "free"),
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "locked"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
                };
            }
            
            // Add the asset to the results list.
            resultsList.Add(new SpotAsset(asset!, free, locked));
            validator.Reset();
        }

        return resultsList;
    }

    // Post new order.
    /// <summary>
    /// Prepares a query for the order of the type <see cref="OrderType.Limit"/> with the specified order parameters.
    /// </summary>
    /// <param name="symbol">The symbol to post the order on.</param>
    /// <param name="side">The order side.</param>
    /// <param name="tif">The time in force for the order.</param>
    /// <param name="price">The limit price to execute the order at.</param>
    /// <param name="quantity">The quantity of the base asset to trade.</param>
    /// <param name="id">The custom identifier for the order (optional).</param>
    /// <param name="orderResponseType">The type of response desired (optional).
    /// The default value is <see cref="OrderResponseType.Full"/>.</param>
    public IDeferredQuery<SpotOrderResponseBase> PreparePostLimitOrder(
        string symbol, OrderSide side, TimeInForce tif, decimal price, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null)
    {
        return PreparePostOrder(
            symbol: symbol,
            side: side,
            type: OrderType.Limit,
            tif: tif,
            price: price,
            stopPrice: null,
            trailingDelta: null,
            quantity: quantity,
            quoteQuantity: null,
            icebergQuantity: null,
            id: id,
            orderResponseType: orderResponseType,
            selfTradePreventionMode: null);
    }

    /// <summary>
    /// Posts a new order of the type <see cref="OrderType.Limit"/> with the specified order parameters.
    /// </summary>
    public Task<SpotOrderResponseBase> PostLimitOrderAsync(
        string symbol, OrderSide side, TimeInForce tif, decimal price, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrderResponseBase> query = PreparePostLimitOrder(
            symbol, side, tif, price, quantity, id, orderResponseType))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for the order of the type <see cref="OrderType.LimitMaker"/> with the specified order parameters.
    /// </summary>
    /// <param name="symbol">The symbol to post the order on.</param>
    /// <param name="side">The order side.</param>
    /// <param name="price">The limit price to execute the order at.</param>
    /// <param name="quantity">The quantity of the base asset to trade.</param>
    /// <param name="id">The custom identifier for the order (optional).</param>
    /// <param name="orderResponseType">The type of response desired (optional).
    /// The default value is <see cref="OrderResponseType.Full"/>.</param>
    public IDeferredQuery<SpotOrderResponseBase> PreparePostLimitMakerOrder(
        string symbol, OrderSide side, decimal price, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null)
    {
        return PreparePostOrder(
            symbol: symbol,
            side: side,
            type: OrderType.LimitMaker,
            tif: null,
            price: price,
            stopPrice: null,
            trailingDelta: null,
            quantity: quantity,
            quoteQuantity: null,
            icebergQuantity: null,
            id: id,
            orderResponseType: orderResponseType,
            selfTradePreventionMode: null);
    }

    /// <summary>
    /// Posts a new order of the type <see cref="OrderType.LimitMaker"/> with the specified order parameters.
    /// </summary>
    public Task<SpotOrderResponseBase> PostLimitMakerOrderAsync(
        string symbol, OrderSide side, decimal price, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrderResponseBase> query = PreparePostLimitMakerOrder(
            symbol, side, price, quantity, id, orderResponseType))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for the order of the type <see cref="OrderType.Market"/> with the specified order parameters.
    /// <para>Either <paramref name="baseQuantity"/> or <paramref name="quoteQuantity"/> must be specified, but not both.</para>
    /// </summary>
    /// <param name="symbol">The symbol to post the order on.</param>
    /// <param name="side">The order side.</param>
    /// <param name="baseQuantity">The quantity of the base asset to trade.</param>
    /// <param name="quoteQuantity">The quantity of the quote asset to trade.</param>
    /// <param name="id">The custom identifier for the order (optional).</param>
    /// <param name="orderResponseType">The type of response desired (optional).
    /// The default value is <see cref="OrderResponseType.Full"/>.</param>
    public IDeferredQuery<SpotOrderResponseBase> PreparePostMarketOrder(
        string symbol, OrderSide side, decimal? baseQuantity, decimal? quoteQuantity,
        string? id = null, OrderResponseType? orderResponseType = null)
    {
        if ((baseQuantity == null) == (quoteQuantity == null))
        {
            throw new ArgumentException(
                $"Exclusively one of the {nameof(baseQuantity)} and {nameof(quoteQuantity)} parameters must be provided " +
                $"with a non-null value. However, the specified values are {baseQuantity} and {quoteQuantity} accordingly.");
        }

        return PreparePostOrder(
            symbol: symbol,
            side: side,
            type: OrderType.Market,
            tif: null,
            price: null,
            stopPrice: null,
            trailingDelta: null,
            quantity: baseQuantity,
            quoteQuantity: quoteQuantity,
            icebergQuantity: null,
            id: id,
            orderResponseType: orderResponseType,
            selfTradePreventionMode: null);
    }

    /// <summary>
    /// Posts a new order of the type <see cref="OrderType.Market"/> with the specified order parameters.
    /// </summary>
    public Task<SpotOrderResponseBase> PostMarketOrderAsync(
        string symbol, OrderSide side, decimal? baseQuantity, decimal? quoteQuantity,
        string? id = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrderResponseBase> query = PreparePostMarketOrder(
            symbol, side, baseQuantity, quoteQuantity, id, orderResponseType))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for the order of the type <see cref="OrderType.StopLossLimit"/> with the specified order parameters.
    /// </summary>
    /// <param name="symbol">The symbol to post the order on.</param>
    /// <param name="side">The order side.</param>
    /// <param name="tif">The time in force for the order.</param>
    /// <param name="stopPrice">The price at which the stop loss order will be triggered.</param>
    /// <param name="executionPrice">The price at which the stop loss order will be executed once it is triggered.</param>
    /// <param name="quantity">The quantity of the base asset to trade.</param>
    /// <param name="id">The custom identifier for the order (optional).</param>
    /// <param name="orderResponseType">The type of response desired (optional).
    /// The default value is <see cref="OrderResponseType.Ack"/>.</param>
    public IDeferredQuery<SpotOrderResponseBase> PreparePostStopLossOrder(string symbol,
        OrderSide side, TimeInForce tif, decimal stopPrice, decimal executionPrice, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null)
    {
        return PreparePostOrder(
            symbol: symbol,
            side: side,
            type: OrderType.StopLossLimit,
            tif: tif,
            price: executionPrice,
            stopPrice: stopPrice,
            trailingDelta: null,
            quantity: quantity,
            quoteQuantity: null,
            icebergQuantity: null,
            id: id,
            orderResponseType: orderResponseType,
            selfTradePreventionMode: null);
    }

    /// <summary>
    /// Posts a new order of the type <see cref="OrderType.StopLossLimit"/> with the specified order parameters.
    /// </summary>
    public Task<SpotOrderResponseBase> PostStopLossOrderAsync(
        string symbol, OrderSide side, TimeInForce tif, decimal stopPrice, decimal executionPrice, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrderResponseBase> query = PreparePostStopLossOrder(
            symbol, side, tif, stopPrice, executionPrice, quantity, id, orderResponseType))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for the order of the type <see cref="OrderType.StopLossLimit"/> with the specified order parameters.
    /// </summary>
    /// <param name="symbol">The symbol to post the order on.</param>
    /// <param name="side">The order side.</param>
    /// <param name="tif">The time in force for the order.</param>
    /// <param name="trailingDelta">
    /// The trailing delta for the order (in BIPS). Specifies the percentage change required to trigger order entry.</param>
    /// <param name="executionPrice">The price to execute the stop order at once the trailing stop is triggered.</param>
    /// <param name="activationPrice">
    /// The activation price for the trailing stop (optional).
    /// <para>If provided, the order will only start tracking price changes after the stop price condition is met.</para>
    /// <para>If omitted, the order starts tracking price changes from the next market trade.</para>
    /// </param>
    /// <param name="quantity">The quantity of the base asset to trade.</param>
    /// <param name="id">The custom identifier for the order (optional).</param>
    /// <param name="orderResponseType">The type of response desired (optional).
    /// The default value is <see cref="OrderResponseType.Ack"/>.</param>
    public IDeferredQuery<SpotOrderResponseBase> PreparePostStopLossOrder(string symbol, OrderSide side,
        TimeInForce tif, uint trailingDelta, decimal? activationPrice, decimal executionPrice, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null)
    {
        return PreparePostOrder(
            symbol: symbol,
            side: side,
            type: OrderType.StopLossLimit,
            tif: tif,
            price: executionPrice,
            stopPrice: activationPrice,
            trailingDelta: trailingDelta,
            quantity: quantity,
            quoteQuantity: null,
            icebergQuantity: null,
            id: id,
            orderResponseType: orderResponseType,
            selfTradePreventionMode: null);
    }

    /// <summary>
    /// Posts a new order of the type <see cref="OrderType.StopLossLimit"/> with the specified order parameters.
    /// </summary>
    public Task<SpotOrderResponseBase> PostStopLossOrderAsync(string symbol, OrderSide side,
        TimeInForce tif, uint trailingDelta, decimal? activationPrice, decimal executionPrice, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrderResponseBase> query = PreparePostStopLossOrder(
            symbol, side, tif, trailingDelta, activationPrice, executionPrice, quantity, id, orderResponseType))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for the order of the type <see cref="OrderType.TakeProfitLimit"/> with the specified order parameters.
    /// </summary>
    /// <param name="symbol">The symbol to post the order on.</param>
    /// <param name="side">The order side.</param>
    /// <param name="tif">The time in force for the order.</param>
    /// <param name="takePrice">The price at which the take profit order will be triggered.</param>
    /// <param name="executionPrice">The price at which the stop loss order will be executed once it is triggered.</param>
    /// <param name="quantity">The quantity of the base asset to trade.</param>
    /// <param name="id">The custom identifier for the order (optional).</param>
    /// <param name="orderResponseType">The type of response desired (optional).
    /// The default value is <see cref="OrderResponseType.Ack"/>.</param>
    public IDeferredQuery<SpotOrderResponseBase> PreparePostTakeProfitOrder(
        string symbol, OrderSide side, TimeInForce tif, decimal takePrice, decimal executionPrice, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null)
    {
        return PreparePostOrder(
            symbol: symbol,
            side: side,
            type: OrderType.TakeProfitLimit,
            tif: tif,
            price: executionPrice,
            stopPrice: takePrice,
            trailingDelta: null,
            quantity: quantity,
            quoteQuantity: null,
            icebergQuantity: null,
            id: id,
            orderResponseType: orderResponseType,
            selfTradePreventionMode: null);
    }

    /// <summary>
    /// Posts a new order of the type <see cref="OrderType.TakeProfitLimit"/> with the specified order parameters.
    /// </summary>
    public Task<SpotOrderResponseBase> PostTakeProfitOrderAsync(
        string symbol, OrderSide side, TimeInForce tif, decimal takePrice, decimal executionPrice, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrderResponseBase> query = PreparePostTakeProfitOrder(
            symbol, side, tif, takePrice, executionPrice, quantity, id, orderResponseType))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for the order of the type <see cref="OrderType.TakeProfitLimit"/> with the specified order parameters.
    /// </summary>
    /// <param name="symbol">The symbol to post the order on.</param>
    /// <param name="side">The order side.</param>
    /// <param name="tif">The time in force for the order.</param>
    /// <param name="trailingDelta">
    /// The trailing delta for the order (in BIPS). Specifies the percentage change required to trigger order entry.</param>
    /// <param name="executionPrice">The price to execute the stop order at once the trailing stop is triggered.</param>
    /// <param name="activationPrice">
    /// The activation price for the trailing stop (optional).
    /// <para>If provided, the order will only start tracking price changes after the stop price condition is met.</para>
    /// <para>If omitted, the order starts tracking price changes from the next market trade.</para>
    /// </param>
    /// <param name="quantity">The quantity of the base asset to trade.</param>
    /// <param name="id">The custom identifier for the order (optional).</param>
    /// <param name="orderResponseType">The type of response desired (optional).
    /// The default value is <see cref="OrderResponseType.Ack"/>.</param>
    public IDeferredQuery<SpotOrderResponseBase> PreparePostTakeProfitOrder(string symbol, OrderSide side,
        TimeInForce tif, uint trailingDelta, decimal? activationPrice, decimal executionPrice, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null)
    {
        return PreparePostOrder(
            symbol: symbol,
            side: side,
            type: OrderType.TakeProfitLimit,
            tif: tif,
            price: executionPrice,
            stopPrice: activationPrice,
            trailingDelta: trailingDelta,
            quantity: quantity,
            quoteQuantity: null,
            icebergQuantity: null,
            id: id,
            orderResponseType: orderResponseType,
            selfTradePreventionMode: null);
    }

    /// <summary>
    /// Posts a new order of the type <see cref="OrderType.TakeProfitLimit"/> with the specified order parameters.
    /// </summary>
    public Task<SpotOrderResponseBase> PostTakeProfitOrderAsync(string symbol, OrderSide side,
        TimeInForce tif, uint trailingDelta, decimal? activationPrice, decimal executionPrice, decimal quantity,
        string? id = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrderResponseBase> query = PreparePostTakeProfitOrder(
            symbol, side, tif, trailingDelta, activationPrice, executionPrice, quantity, id, orderResponseType))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for the iceberg order of the type <see cref="OrderType.Limit"/> with the specified order parameters.
    /// </summary>
    /// <param name="symbol">The symbol to post the order on.</param>
    /// <param name="side">The order side.</param>
    /// <param name="tif">The time in force for the order.</param>
    /// <param name="price">The limit price to execute the order at.</param>
    /// <param name="totalQuantity">The total quantity of the base asset to trade.</param>
    /// <param name="partQuantity">The quantity of the base asset to trade with each iceberg part.</param>
    /// <param name="id">The custom identifier for the order (optional).</param>
    /// <param name="orderResponseType">The type of response desired (optional).
    /// The default value is <see cref="OrderResponseType.Full"/>.</param>
    public IDeferredQuery<SpotOrderResponseBase> PreparePostIcebergLimitOrder(
        string symbol, OrderSide side, TimeInForce tif, decimal price, decimal totalQuantity, decimal partQuantity,
        string? id = null, OrderResponseType? orderResponseType = null)
    {
        return PreparePostOrder(
            symbol: symbol,
            side: side,
            type: OrderType.Limit,
            tif: tif,
            price: price,
            stopPrice: null,
            trailingDelta: null,
            quantity: totalQuantity,
            quoteQuantity: null,
            icebergQuantity: partQuantity,
            id: id,
            orderResponseType: orderResponseType,
            selfTradePreventionMode: null);
    }

    /// <summary>
    /// Posts a new iceberg order of the type <see cref="OrderType.Limit"/> with the specified order parameters.
    /// </summary>
    public Task<SpotOrderResponseBase> PostIcebergLimitOrderAsync(
        string symbol, OrderSide side, TimeInForce tif, decimal price, decimal totalQuantity, decimal partQuantity,
        string? id = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrderResponseBase> query = PreparePostIcebergLimitOrder(
            symbol, side, tif, price, totalQuantity, partQuantity, id, orderResponseType))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private IDeferredQuery<SpotOrderResponseBase> PreparePostOrder(
        string symbol, OrderSide side, OrderType type, TimeInForce? tif,
        decimal? price, decimal? stopPrice, uint? trailingDelta,
        decimal? quantity, decimal? quoteQuantity, decimal? icebergQuantity, 
        string? id, OrderResponseType? orderResponseType, SelfTradePreventionMode? selfTradePreventionMode)
    {
        ThrowIfNotRunning();
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));
        if (price <= 0.0m)
            throw new ArgumentOutOfRangeException(nameof(price));
        if (stopPrice <= 0.0m)
            throw new ArgumentOutOfRangeException(nameof(stopPrice));
        if (quantity <= 0.0m)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quoteQuantity <= 0.0m)
            throw new ArgumentOutOfRangeException(nameof(quoteQuantity));
        if (icebergQuantity <= 0.0m)
            throw new ArgumentOutOfRangeException(nameof(icebergQuantity));
        if (id != null && String.IsNullOrWhiteSpace(id))
            throw new ArgumentException("The specified custom ID is empty.", nameof(id));

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
            new QueryWeight(GetWeightDimensionId(RateLimitType.UID), 1),
        };

        QueryBuilder qs = new QueryBuilder(327);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        qs.AddParameter("side", Format(side));
        qs.AddParameter("type", Format(type));
        if (tif != null)
        {
            qs.AddParameter("timeInForce", Format(tif.Value));
        }
        if (price != null)
        {
            qs.AddParameter("price", price.Value);
        }
        if (stopPrice != null)
        {
            qs.AddParameter("stopPrice", stopPrice.Value);
        }
        if (trailingDelta != null)
        {
            qs.AddParameter("trailingDelta", trailingDelta.Value);
        }
        if (quantity != null)
        {
            qs.AddParameter("quantity", quantity.Value);
        }
        if (quoteQuantity != null)
        {
            qs.AddParameter("quoteOrderQty", quoteQuantity.Value);
        }
        if (icebergQuantity != null)
        {
            qs.AddParameter("icebergQty", icebergQuantity.Value);
        }
        if (id != null)
        {
            qs.AddParameter("newClientOrderId", id);
        }
        if (orderResponseType != null)
        {
            qs.AddParameter("newOrderRespType", Format(orderResponseType.Value));
        }
        if (selfTradePreventionMode != null)
        {
            qs.AddParameter("selfTradePreventionMode", SpotUtility.Format(selfTradePreventionMode.Value));
        }

        OrderResponseType expectedRspType;
        if (orderResponseType != null)
        {
            expectedRspType = orderResponseType.Value;
        }
        else
        {
            expectedRspType = type == OrderType.Limit || type == OrderType.LimitMaker || type == OrderType.Market?
                OrderResponseType.Full : OrderResponseType.Ack;
        }

        return new DeferredQuery<SpotOrderResponseBase>(
            query: new QueryParams(HttpMethod.POST, RESTEndpoint.Url, PostNewOrderEndpoint, qs, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParsePostOrderResponse,
            parseArgs: expectedRspType,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    private SpotOrderResponseBase ParsePostOrderResponse(byte[] data, object? parseArgs)
    {
        return (OrderResponseType)parseArgs! switch
        {
            OrderResponseType.Ack => ParsePostOrderAckResponse(data),
            OrderResponseType.Result => ParsePostOrderResultResponse(data),
            OrderResponseType.Full => ParsePostOrderFullResponse(data),
            _ => throw new NotImplementedException($"The post order response type \"{parseArgs}\" is not implemented."),
        };
    }

    private SpotOrderResponseAck ParsePostOrderAckResponse(byte[] data)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);

        SpotOrderResponseAck response = new SpotOrderResponseAck();
        ParseSchemaValidator validator = new ParseSchemaValidator(3);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            switch (propName)
            {
                case "symbol":
                    response.Symbol = ParseUtility.GetNonEmptyString(ref reader, propName);
                    validator.RegisterProperty(0);
                    break;
                case "orderId":
                    response.OrderId = reader.GetInt64();
                    validator.RegisterProperty(1);
                    break;
                case "orderListId":
                    response.OrderListId = reader.GetInt64();
                    break;
                case "clientOrderId":
                    response.ClientOrderId = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "transactTime":
                    response.TransactionTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    validator.RegisterProperty(2);
                    break;
                default:
                    PostLogMessage(
                        LogLevel.Warning,
                        $"An unknown property \"{propName}\" of the post order response was encountered.");
                    reader.Skip();
                    break;
            }
        }

        if (!validator.IsComplete())
        {
            const string objName = "post order response";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "order ID"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "transaction time"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
            };
        }
        
        return response;
    }

    private SpotOrderResponseRes ParsePostOrderResultResponse(byte[] data)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);

        SpotOrderResponseRes response = new SpotOrderResponseRes();
        ParseSchemaValidator validator = new ParseSchemaValidator(7);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
            {
                throw ParseUtility.GenerateNoPropertyValueException(propName);
            }

            switch (propName)
            {
                case "symbol":
                    response.Symbol = ParseUtility.GetNonEmptyString(ref reader, propName);
                    validator.RegisterProperty(0);
                    break;
                case "orderId":
                    response.OrderId = reader.GetInt64();
                    validator.RegisterProperty(1);
                    break;
                case "orderListId":
                    response.OrderListId = reader.GetInt64();
                    break;
                case "clientOrderId":
                    response.ClientOrderId = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "transactTime":
                    response.TransactionTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    validator.RegisterProperty(2);
                    break;
                case "price":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal price);
                    response.Price = price;
                    break;
                case "stopPrice":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal stopPrice);
                    response.StopPrice = stopPrice;
                    break;
                case "origQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal origBaseQt);
                    response.OriginalBaseQuantity = origBaseQt;
                    break;
                case "executedQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out response.ExecutedBaseQuantity);
                    break;
                case "cummulativeQuoteQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal cumQuoteQt);
                    if (cumQuoteQt >= 0.0m) response.CummulativeQuoteQuantity = cumQuoteQt;
                    break;
                case "status":
                    response.OrderStatus = ParseUtility.ParseOrderStatus(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(3);
                    break;
                case "timeInForce":
                    response.TimeInForce = ParseTimeInForce(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(4);
                    break;
                case "type":
                    response.OrderType = ParseOrderType(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(5);
                    break;
                case "side":
                    response.OrderSide = ParseUtility.ParseOrderSide(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(6);
                    break;
                case "strategyId":
                    response.StrategyId = reader.GetInt32();
                    break;
                case "strategyType":
                    response.StrategyType = reader.GetInt32();
                    break;
                case "workingTime":
                    response.WorkingTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    break;
                case "selfTradePreventionMode":
                    response.STPMode = SpotUtility.ParseSelfTradePreventionMode(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    break;
                default:
                    PostLogMessage(
                        LogLevel.Warning,
                        $"An unknown property \"{propName}\" of the post order response was encountered.");
                    reader.Skip();
                    break;
            }
        }

        if (!validator.IsComplete())
        {
            const string objName = "post order response";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "order ID"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "transaction time"),
                3 => ParseUtility.GenerateMissingPropertyException(objName, "order status"),
                4 => ParseUtility.GenerateMissingPropertyException(objName, "time in force"),
                5 => ParseUtility.GenerateMissingPropertyException(objName, "order type"),
                6 => ParseUtility.GenerateMissingPropertyException(objName, "side"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
            };
        }

        return response;
    }

    private SpotOrderResponseFull ParsePostOrderFullResponse(byte[] data)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);

        SpotOrderResponseFull response = new SpotOrderResponseFull();
        ParseSchemaValidator validator = new ParseSchemaValidator(7);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
            {
                throw ParseUtility.GenerateNoPropertyValueException(propName);
            }

            // Parse order response properties.
            switch (propName)
            {
                case "symbol":
                    response.Symbol = ParseUtility.GetNonEmptyString(ref reader, propName);
                    validator.RegisterProperty(0);
                    break;
                case "orderId":
                    response.OrderId = reader.GetInt64();
                    validator.RegisterProperty(1);
                    break;
                case "orderListId":
                    response.OrderListId = reader.GetInt64();
                    break;
                case "clientOrderId":
                    response.ClientOrderId = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "transactTime":
                    response.TransactionTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    validator.RegisterProperty(2);
                    break;
                case "price":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal price);
                    response.Price = price;
                    break;
                case "stopPrice":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal stopPrice);
                    response.StopPrice = stopPrice;
                    break;
                case "origQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal origBaseQt);
                    response.OriginalBaseQuantity = origBaseQt;
                    break;
                case "executedQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out response.ExecutedBaseQuantity);
                    break;
                case "cummulativeQuoteQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal cumQuoteQt);
                    if (cumQuoteQt >= 0.0m) response.CummulativeQuoteQuantity = cumQuoteQt;
                    break;
                case "status":
                    response.OrderStatus = ParseUtility.ParseOrderStatus(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(3);
                    break;
                case "timeInForce":
                    response.TimeInForce = ParseTimeInForce(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(4);
                    break;
                case "type":
                    response.OrderType = ParseOrderType(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(5);
                    break;
                case "side":
                    response.OrderSide = ParseUtility.ParseOrderSide(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(6);
                    break;
                case "strategyId":
                    response.StrategyId = reader.GetInt32();
                    break;
                case "workingTime":
                    response.WorkingTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    break;
                case "selfTradePreventionMode":
                    response.STPMode = SpotUtility.ParseSelfTradePreventionMode(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    break;
                case "fills":
                    response.Fills = ParseUtility.ParseOrderPartialFills(ref reader);
                    break;
                default:
                    PostLogMessage(
                        LogLevel.Warning,
                        $"An unknown property \"{propName}\" of the post order response was encountered.");
                    reader.Skip();
                    break;
            }
        }

        // Check whether all the essential properties were provided.
        if (!validator.IsComplete())
        {
            const string objName = "post order response";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "order ID"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "transaction time"),
                3 => ParseUtility.GenerateMissingPropertyException(objName, "order status"),
                4 => ParseUtility.GenerateMissingPropertyException(objName, "time in force"),
                5 => ParseUtility.GenerateMissingPropertyException(objName, "order type"),
                6 => ParseUtility.GenerateMissingPropertyException(objName, "side"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
            };
        }

        // Return the result.
        return response;
    }

    // Cancel order.
    /// <summary>
    /// Prepares a query for cancellation of the order with the specified unique ID.
    /// </summary>
    /// <param name="symbol">The symbol that the order was placed on.</param>
    /// <param name="orderId">The global numerical identifier of the order to cancel.</param>
    public IDeferredQuery<SpotOrder> PrepareCancelOrder(string symbol, long orderId)
    {
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        return PrepareCancelOrder(
            symbol: symbol,
            orderId: orderId,
            origClientOrderId: null,
            newClientOrderId: null,
            restriction: null);
    }

    /// <summary>
    /// Cancels the order with the specified unique ID.
    /// </summary>
    public Task<SpotOrder> CancelOrderAsync(string symbol, long orderId, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrder> query = PrepareCancelOrder(symbol, orderId))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for cancellation of the order with the specified custom ID.
    /// </summary>
    /// <param name="symbol">The symbol that the order was placed on.</param>
    /// <param name="clientOrderId">The original custom identifier of the order to cancel.</param>
    public IDeferredQuery<SpotOrder> PrepareCancelOrder(string symbol, string clientOrderId)
    {
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));
        if (String.IsNullOrWhiteSpace(clientOrderId))
            throw new ArgumentNullException(nameof(clientOrderId));

        return PrepareCancelOrder(
            symbol: symbol,
            orderId: null,
            origClientOrderId: clientOrderId,
            newClientOrderId: null,
            restriction: null);
    }

    /// <summary>
    /// Cancels the order with the specified custom ID.
    /// </summary>
    public Task<SpotOrder> CancelOrderAsync(string symbol, string clientOrderId, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrder> query = PrepareCancelOrder(symbol, clientOrderId))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private IDeferredQuery<SpotOrder> PrepareCancelOrder(
        string symbol,
        long? orderId,
        string? origClientOrderId,
        string? newClientOrderId,
        CancellationRestriction? restriction)
    {
        ThrowIfNotRunning();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
        };

        QueryBuilder qs = new QueryBuilder(235);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        if (orderId != null)
        {
            qs.AddParameter("orderId", orderId.Value);
        }
        else
        {
            qs.AddParameter("origClientOrderId", origClientOrderId!);
        }
        if (newClientOrderId != null)
        {
            qs.AddParameter("newClientOrderId", newClientOrderId);
        }
        if (restriction != null)
        {
            qs.AddParameter("cancelRestrictions", Format(restriction.Value));
        }

        return new DeferredQuery<SpotOrder>(
            query: new QueryParams(HttpMethod.DELETE, RESTEndpoint.Url, DeleteOrderEndpoint, qs, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseOrder,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    // Cancel all open orders.
    /// <summary>
    /// Prepares a query for cancellation of all active orders on a symbol, including OCO orders.
    /// </summary>
    /// <param name="symbol">A symbol to cancel orders on.</param>
    public IDeferredQuery<List<SpotOrder>> PrepareCancelAllOrders(string symbol)
    {
        ThrowIfNotRunning();
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
        };

        QueryBuilder qs = new QueryBuilder(138);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));

        return new DeferredQuery<List<SpotOrder>>(
            query: new QueryParams(HttpMethod.DELETE, RESTEndpoint.Url, DeleteAllOpenOrdersEndpoint, qs, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseOrderList,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    // Get order.
    /// <summary>
    /// Prepares a query for info on an order (either active or historical) with the specified unique id.
    /// </summary>
    /// <param name="symbol">The symbol that the order was placed on.</param>
    /// <param name="orderId">The unique numerical identifier of the order to fetch.</param>
    public IDeferredQuery<SpotOrder> PrepareGetOrder(string symbol, long orderId)
    {
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        return PrepareGetOrder(
            symbol: symbol,
            orderId: orderId,
            origClientOrderId: null);
    }

    /// <summary>
    /// Prepares a query for info on an order (either active or historical) with the specified custom id.
    /// </summary>
    /// <param name="symbol">The symbol that the order was placed on.</param>
    /// <param name="clientOrderId">The original custom identifier of the order to fetch.</param>
    public IDeferredQuery<SpotOrder> PrepareGetOrder(string symbol, string clientOrderId)
    {
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));
        if (String.IsNullOrWhiteSpace(clientOrderId))
            throw new ArgumentNullException(nameof(clientOrderId));

        return PrepareGetOrder(
            symbol: symbol,
            orderId: null,
            origClientOrderId: clientOrderId);
    }

    /// <summary>
    /// Gets the info on an order (either active or historical) with the specified unique ID.
    /// </summary>
    public Task<SpotOrder> GetOrderAsync(string symbol, long orderId, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrder> query = PrepareGetOrder(symbol, orderId))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private IDeferredQuery<SpotOrder> PrepareGetOrder(string symbol, long? orderId, string? origClientOrderId)
    {
        ThrowIfNotRunning();
        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 2),
        };

        QueryBuilder qs = new QueryBuilder(195);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        if (orderId != null)
        {
            qs.AddParameter("orderId", orderId.Value);
        }
        else
        {
            qs.AddParameter("origClientOrderId", origClientOrderId!);
        }

        return new DeferredQuery<SpotOrder>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetOrderEndpoint, qs, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseOrder,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets the info on an order (either active or historical) with the specified custom ID.
    /// </summary>
    public Task<SpotOrder> GetOrderAsync(string symbol, string clientOrderId, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotOrder> query = PrepareGetOrder(symbol, clientOrderId))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get all orders.
    /// <summary>
    /// Prepares a query for all recent orders on the specified symbol (active, canceled, or filled).
    /// </summary>
    /// <param name="symbol">The symbol to get orders on.</param>
    /// <param name="limit">The maximum number of the items to fetch. The maximum is 1000, the default is 500.</param>
    /// <returns></returns>
    public IDeferredQuery<List<SpotOrder>> PrepareGetOrders(string symbol, int? limit)
    {
        return PrepareGetOrders(
            symbol: symbol,
            fromId: null,
            startTime: null,
            endTime: null,
            limit: limit);
    }

    /// <summary>
    /// Gets all recent orders on the specified symbol (active, canceled, or filled).
    /// </summary>
    public Task<List<SpotOrder>> GetOrdersAsync(string symbol, int? limit, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<SpotOrder>> query = PrepareGetOrders(symbol, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for all orders on the specified symbol (active, canceled, or filled),
    /// starting from a specific order.
    /// </summary>
    /// <param name="symbol">The symbol to get orders on.</param>
    /// <param name="fromId">The unique order identifier to fetch orders from.</param>
    /// <param name="limit">The maximum number of the items to fetch. The maximum is 1000, the default is 500.</param>
    /// <returns></returns>
    public IDeferredQuery<List<SpotOrder>> PrepareGetOrders(string symbol, long fromId, int? limit)
    {
        return PrepareGetOrders(
            symbol: symbol,
            fromId: fromId,
            startTime: null,
            endTime: null,
            limit: limit);
    }

    /// <summary>
    /// Gets all orders on the specified symbol (active, canceled, or filled), starting from a specific order.
    /// </summary>
    public Task<List<SpotOrder>> GetOrdersAsync(string symbol, long fromId, int? limit, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<SpotOrder>> query = PrepareGetOrders(symbol, fromId, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for all orders on the specified symbol within the specified time range (active, canceled, or filled).
    /// <para>Note: for historical orders the info on the cummulative quote quantity may be unavailable.</para>
    /// </summary>
    /// <param name="symbol">The symbol to get orders on.</param>
    /// <param name="startTime">The time to fetch orders from.</param>
    /// <param name="endTime">The time to fetch orders prior to.</param>
    /// <param name="limit">The maximum number of the items to fetch. The maximum is 1000, the default is 500.</param>
    /// <returns></returns>
    public IDeferredQuery<List<SpotOrder>> PrepareGetOrders(
        string symbol, DateTime? startTime, DateTime? endTime, int? limit)
    {
        return PrepareGetOrders(
            symbol: symbol,
            fromId: null,
            startTime: startTime,
            endTime: endTime,
            limit: limit);
    }

    /// <summary>
    /// Gets all orders on the specified symbol within the specified time range (active, canceled, or filled).
    /// </summary>
    public Task<List<SpotOrder>> GetOrdersAsync(
        string symbol, DateTime? startTime, DateTime? endTime, int? limit, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<SpotOrder>> query = PrepareGetOrders(symbol, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private IDeferredQuery<List<SpotOrder>> PrepareGetOrders(
        string symbol, long? fromId, DateTime? startTime, DateTime? endTime, int? limit)
    {
        ThrowIfNotRunning();
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));
        if (startTime > endTime)
            throw new ArgumentException(
                $"The specified combination of the start time ({startTime}) and the end time ({endTime}) is invalid.");

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 10),
        };

        QueryBuilder qs = new QueryBuilder(178);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        if (fromId != null)
        {
            qs.AddParameter("orderId", fromId.Value);
        }
        if (startTime != null)
        {
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        }
        if (endTime != null)
        {
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        }
        if (limit != null)
        {
            qs.AddParameter("limit", limit.Value);
        }

        return new DeferredQuery<List<SpotOrder>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAllOrdersEndpoint, qs, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseOrderList,
            parseArgs: limit != null ? limit.Value : 64,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    // Get all open orders.
    /// <summary>
    /// Prepares a query for all open orders on all symbols.
    /// <para>Note: This query is rate limit intensive.</para>
    /// </summary>
    public IDeferredQuery<List<SpotOrder>> PrepareGetAllOpenOrders()
    {
        ThrowIfNotRunning();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 40),
        };

        return new DeferredQuery<List<SpotOrder>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAllOpenOrdersEndpoint, null, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseOrderList,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets all open orders on all symbols.
    /// <para>Note: This query is rate limit intensive.</para>
    /// </summary>
    public Task<List<SpotOrder>> GetAllOpenOrdersAsync(CancellationToken ct = default)
    {
        using (IDeferredQuery<List<SpotOrder>> query = PrepareGetAllOpenOrders())
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for all open orders on the specified symbol.
    /// </summary>
    /// <param name="symbol">The symbol to list open orders on.</param>
    public IDeferredQuery<List<SpotOrder>> PrepareGetAllOpenOrders(string symbol)
    {
        ThrowIfNotRunning();
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 3),
        };

        QueryBuilder qs = new QueryBuilder(137);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));

        return new DeferredQuery<List<SpotOrder>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAllOpenOrdersEndpoint, qs, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseOrderList,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets all open orders on the specified symbol.
    /// </summary>
    /// <param name="symbol">The symbol to list open orders on.</param>
    public Task<List<SpotOrder>> GetAllOpenOrdersAsync(string symbol, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<SpotOrder>> query = PrepareGetAllOpenOrders(symbol))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Account trade list.
    /// <summary>
    /// Creates a deferred query for user trades on a specific symbol.
    /// <para>Note: there's no principal difference in processing time between requests
    /// with either <paramref name="startTime"/> or <paramref name="fromId"/> specified.</para>
    /// </summary>
    /// <param name="symbol">The symbol to list trades on.</param>
    /// <param name="orderId">The ID of the order to get trades of.</param>
    /// <param name="startTime">The time to get trades from (inclusive).</param>
    /// <param name="endTime">The time to get trades prior to (inclusive).
    /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified, 
    /// the period between them must not be longer than <see cref="AccountTradesMaxTimeRange"/> (24 hours).</para></param>
    /// <param name="fromId">The trade ID to fetch data from (inclusive). Cannot be used in combination with <paramref name="limit"/>.</param>
    /// <param name="limit">A maximum number of trades to fetch. The default value is 500, the maximum value is 1000.</param>
    public IDeferredQuery<List<SpotTrade>> PrepareGetAccountTrades(
        string symbol, long? orderId = null,
        DateTime? startTime = null, DateTime? endTime = null, long? fromId = null, int? limit = null)
    {
        ThrowIfNotRunning();
        if (String.IsNullOrWhiteSpace(symbol))
            throw new ArgumentNullException(nameof(symbol));

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 10),
        };

        QueryBuilder qs = new QueryBuilder(176);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        if (orderId != null)
        {
            qs.AddParameter("orderId", orderId.Value);
        }
        if (startTime != null)
        {
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        }
        if (endTime != null)
        {
            if (startTime != null && (endTime.Value - startTime.Value).Ticks < TimeSpan.TicksPerMillisecond)
                throw new ArgumentException("The end time must be later than the start one.");
            else if ((endTime.Value - startTime!.Value).Ticks > AccountTradesMaxTimeRange)
                throw new ArgumentException("A period between the start time and the end time cannot be longer than 24 hours.");
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        }
        if (fromId != null)
        {
            if (fromId < 0)
                throw new ArgumentOutOfRangeException(nameof(fromId));
            if (limit != null)
                throw new ArgumentException("The trades limit cannot be specified along with the initial trade id.");
            qs.AddParameter("fromId", fromId.Value);
        }
        if (limit != null)
        {
            qs.AddParameter("limit", Math.Clamp(limit.Value, 1, 1000));
        }

        return new DeferredQuery<List<SpotTrade>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAccountTradeListEndpoint, qs, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseAccountTradeList,
            parseArgs: limit != null ? (object)limit.Value : null,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets user trades on a specific symbol.
    /// </summary>
    public Task<List<SpotTrade>> GetAccountTradesAsync(
        string symbol, long? orderId = null, DateTime? startTime = null, DateTime? endTime = null,
        long? fromId = null, int? limit = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<SpotTrade>> query =
            PrepareGetAccountTrades(symbol, orderId, startTime, endTime, fromId, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<SpotTrade> ParseAccountTradeList(byte[] data, object? parseArgs = null)
    {
        List<SpotTrade> results = new List<SpotTrade>(parseArgs is int expectedCount ? expectedCount : 500);
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadArrayStart(ref reader);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ParseUtility.EnsureObjectStartToken(ref reader);
            SpotTrade trade = default;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                if (!reader.Read())
                {
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                }

                switch (propName)
                {
                    case "symbol":
                        trade.Symbol = ParseUtility.GetNonEmptyString(ref reader, propName);
                        break;
                    case "id":
                        trade.Id = reader.GetInt64();
                        break;
                    case "orderId":
                        trade.OrderId = reader.GetInt64();
                        break;
                    case "orderListId":
                        trade.OrderListId = reader.GetInt64();
                        break;
                    case "price":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out trade.Price);
                        break;
                    case "qty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out trade.Quantity);
                        break;
                    case "quoteQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out trade.QuoteQuantity);
                        break;
                    case "commission":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out trade.Commission);
                        break;
                    case "commissionAsset":
                        trade.CommissionAsset = ParseUtility.GetNonEmptyString(ref reader, propName);
                        break;
                    case "time":
                        trade.Time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        break;
                    case "isBuyer":
                        trade.IsBuyer = reader.GetBoolean();
                        break;
                    case "isMaker":
                        trade.IsMaker = reader.GetBoolean();
                        break;
                    case "isBestMatch":
                        trade.IsBestMatch = reader.GetBoolean();
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            results.Add(trade);
        }

        return results;
    }

    // Get current order limit usage.
    /// <summary>
    /// Prepares a query for a user's current spot order count usage for all limit intervals.
    /// </summary>
    public IDeferredQuery<List<RateLimiter>> PrepareGetOrderLimitUsage()
    {
        ThrowIfNotRunning();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 20),
        };

        return new DeferredQuery<List<RateLimiter>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetOrderLimitUsageEndpoint, null, true),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseRateLimiters,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets a user's current spot order count usage for all limit intervals asynchrnously.
    /// </summary>
    public Task<List<RateLimiter>> GetOrderLimitUsageAsync(CancellationToken ct)
    {
        using (IDeferredQuery<List<RateLimiter>> query = PrepareGetOrderLimitUsage())
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<RateLimiter> ParseRateLimiters(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<RateLimiter> limiters = new List<RateLimiter>(6);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            limiters.Add(ParseRateLimiter(ref reader));
        }

        return limiters;
    }

    // Orders parse logic.
    private List<SpotOrder> ParseOrderList(byte[] data, object? parseArgs = null)
    {
        List<SpotOrder> orders = new List<SpotOrder>(parseArgs is int expectedCount ? expectedCount : 32);
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        
        ParseUtility.ReadArrayStart(ref reader);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            orders.Add(ParseOrder(ref reader));
        }
        return orders;
    }

    private SpotOrder ParseOrder(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);
        return ParseOrder(ref reader);
    }

    private SpotOrder ParseOrder(ref Utf8JsonReader reader)
    {
        ParseUtility.EnsureObjectStartToken(ref reader);

        SpotOrder order = new SpotOrder();
        ParseSchemaValidator validator = new ParseSchemaValidator(6);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
                throw new JsonException($"A value of the property \"{propName}\" was expected but the end of the data was reached.");
            switch (propName)
            {
                case "symbol":
                    order.Symbol = ParseUtility.GetNonEmptyString(ref reader, propName);
                    validator.RegisterProperty(0);
                    break;
                case "orderId":
                    order.OrderId = reader.GetInt64();
                    validator.RegisterProperty(1);
                    break;
                case "orderListId":
                    order.OrderListId = reader.GetInt64();
                    break;
                case "clientOrderId":
                    order.ClientOrderId = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "price":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal price);
                    order.Price = price;
                    break;
                case "origQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal origQt);
                    order.OriginalBaseQuantity = origQt;
                    break;
                case "executedQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out order.ExecutedBaseQuantity);
                    break;
                case "cummulativeQuoteQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal cumQuoteQt);
                    if (cumQuoteQt >= 0.0m) order.CummulativeQuoteQuantity = cumQuoteQt;
                    break;
                case "status":
                    order.Status = ParseUtility.ParseOrderStatus(ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(2);
                    break;
                case "timeInForce":
                    order.TimeInForce = ParseTimeInForce(ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(3);
                    break;
                case "type":
                    order.Type = ParseOrderType(ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(4);
                    break;
                case "side":
                    order.Side = ParseUtility.ParseOrderSide(ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(5);
                    break;
                case "stopPrice":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal stopPrice);
                    order.StopPrice = stopPrice;
                    break;
                case "icebergQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal icebergQt);
                    order.IcebergQuantity = icebergQt;
                    break;
                case "time":
                    order.Time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    break;
                case "updateTime":
                    order.UpdateTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    break;
                case "isWorking":
                    order.IsWorking = reader.GetBoolean();
                    break;
                case "workingTime":
                    order.WorkingTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    break;
                case "origQuoteOrderQty":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal origQuoteQt);
                    order.OriginalQuoteQuantity = origQuoteQt;
                    break;
                case "selfTradePreventionMode":
                    order.STPMode = SpotUtility.ParseSelfTradePreventionMode(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    break;
                case "preventedMatchId":
                    order.PreventedMatchId = reader.GetInt64();
                    break;
                case "preventedQuantity":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal pq);
                    order.PreventedQuantity = pq;
                    break;
                case "accountId":
                    break;
                case "origClientOrderId":
                    break;
                default:
                    PostLogMessage(
                        LogLevel.Warning,
                        $"An unknown order property \"{propName}\" was encountered.");
                    reader.Skip();
                    break;
            }
        }

        if (!validator.IsComplete())
        {
            const string objName = "order";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "order ID"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "order status"),
                3 => ParseUtility.GenerateMissingPropertyException(objName, "time in force"),
                4 => ParseUtility.GenerateMissingPropertyException(objName, "order type"),
                5 => ParseUtility.GenerateMissingPropertyException(objName, "side"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
            };
        }

        return order;
    }

    #endregion
}