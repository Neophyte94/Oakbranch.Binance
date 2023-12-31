﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Exceptions;
using Oakbranch.Binance.Utility;
using Oakbranch.Binance.Models;
using Oakbranch.Binance.Models.Spot;
using Oakbranch.Binance.Models.Filters.Exchange;
using Oakbranch.Binance.Models.Filters.Symbol;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Clients;

public class SpotMarketApiClient : ApiV3ClientBase
{
    #region Constants

    // Common.
    /// <summary>
    /// Defines the expected number of spot symbols reported by the server.
    /// <para>Note: there were 2145 symbols as of 09.02.2023</para>
    /// </summary>
    public const int ExpectedSymbolsCount = 2500;

    // Constraints.
    /// <summary>
    /// The maximum number of items that can be fetched in a single trades query.
    /// </summary>
    public const int MaxTradesQueryLimit = 1000;
    /// <summary>
    /// The default number of items that can be fetched in a single trades query.
    /// </summary>
    public const int DefaultTradesQueryLimit = 500;
    /// <summary>
    /// The maximum number of items that can be fetched in a single candlesticks query.
    /// </summary>
    public const int MaxKlinesQueryLimit = 1000;
    /// <summary>
    /// The default number of items that can be fetched in a single candlesticks query.
    /// </summary>
    public const int DefaultKlinesQueryLimit = 500;

    // Endpoints.
    private const string GetServerTimeEndpoint = "/api/v3/time";
    private const string GetOrderBookEndpoint = "/api/v3/depth";
    private const string GetRecentTradesEndpoint = "/api/v3/trades";
    private const string GetOldTradesEndpoint = "/api/v3/historicalTrades";
    private const string GetAggregateTradesEndpoint = "/api/v3/aggTrades";
    private const string GetCandlestickDataEndpoint = "/api/v3/klines";
    private const string GetUIKlinesEndpoint = "/api/v3/uiKlines";
    private const string GetCurrentAveragePriceEndpoint = "/api/v3/avgPrice";
    private const string GetDailyPriceChangeStatsEndpoint = "/api/v3/ticker/24hr";
    private const string GetSymbolPriceTickerEndpoint = "/api/v3/ticker/price";
    private const string GetSymbolOrderBookTickerEndpoint = "/api/v3/ticker/bookTicker";
    private const string GetPriceChangeStatsEndpoint = "/api/v3/ticker";

    #endregion

    #region Instance constructors

    public SpotMarketApiClient(
        IApiConnector connector,
        IRateLimitsRegistry limitsRegistry,
        ILogger<SpotMarketApiClient>? logger = null)
        : base(connector, limitsRegistry, logger)
    { }

    #endregion

    #region Instance methods

    // Get server time.
    /// <summary>
    /// Creates a deferred query to get the current server time.
    /// </summary>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<DateTime> PrepareCheckServerTime()
    {
        ThrowIfNotRunning();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
        };

        return new DeferredQuery<DateTime>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetServerTimeEndpoint, null, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseServerTimeResponse,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets the current server time.
    /// </summary>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="QueryException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task<DateTime> CheckServerTimeAsync(CancellationToken ct)
    {
        using (IDeferredQuery<DateTime> query = PrepareCheckServerTime())
        {
            return query.ExecuteAsync(ct);
        }
    }

    private DateTime ParseServerTimeResponse(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadObjectStart(ref reader);

        string propName = ParseUtility.ReadNonEmptyPropertyName(ref reader);
        if (propName != "serverTime")
        {
            throw new JsonException($"The server time property was expected but \"{propName}\" encountered.");
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"The server time value (number) was expected but \"{reader.TokenType}\" encountered.");
        }
        long ms = reader.GetInt64();

        ParseUtility.ReadObjectEnd(ref reader);

        return CommonUtility.ConvertToDateTime(ms);
    }

    // Get exchange info.
    /// <summary>
    /// Creates a deferred query for information on the spot exchange, including all existing symbols.
    /// </summary>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<SpotExchangeInfo> PrepareGetExchangeInfo() => PrepareGetExchangeInfo(null);

    /// <summary>
    /// Creates a deferred query for information on the spot exchange, limiting to the specified symbols.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<SpotExchangeInfo> PrepareGetExchangeInfo(params string[]? symbols)
    {
        ThrowIfNotRunning();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 20),
        };

        QueryBuilder? qs = null;
        if (symbols != null && symbols.Length != 0)
        {
            int emptyIdx = CommonUtility.IndexOfEmpty(symbols);
            if (emptyIdx != -1)
            {
                throw new ArgumentException(
                    $"The item {emptyIdx} in the specified array is either null or empty.",
                    nameof(symbols));
            }

            if (symbols.Length == 1)
            {
                qs = new QueryBuilder(21);
                qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbols[0]));
            }
            else
            {
                qs = new QueryBuilder(11 + 11 * symbols.Length);
                qs.AddParameter("symbols", symbols.Select<string, string>(CommonUtility.NormalizeSymbol));
            }
        }

        return new DeferredQuery<SpotExchangeInfo>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetExchangeInfoEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseExchangeInfoResponse,
            parseArgs: symbols != null ? symbols.Length : null,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets information on the spot exchange, limiting to the specified symbols, asynchronously.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="QueryException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task<SpotExchangeInfo> GetExchangeInfoAsync(string[]? symbols = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<SpotExchangeInfo> query = PrepareGetExchangeInfo(symbols))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private SpotExchangeInfo ParseExchangeInfoResponse(byte[] data, object? parsingArgs = null)
    {
        int expectedCount = parsingArgs is int ec ? ec : ExpectedSymbolsCount;

        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);

        SpotExchangeInfo ei = new SpotExchangeInfo();
        ParseSchemaValidator validator = new ParseSchemaValidator(4);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
                throw ParseUtility.GenerateNoPropertyValueException(propName);

            switch (propName)
            {
                case "timezone":
                    string timezone = ParseUtility.GetNonEmptyString(ref reader, propName);
                    ei.Timezone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                    validator.RegisterProperty(0);
                    break;
                case "serverTime":
                    ei.ServerTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    validator.RegisterProperty(1);
                    break;
                case "rateLimits":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    List<RateLimiter> limits = new List<RateLimiter>(6);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        limits.Add(ParseRateLimiter(ref reader));
                    }
                    ei.RateLimits = limits;
                    validator.RegisterProperty(2);
                    break;
                case "exchangeFilters":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    List<ExchangeFilter> filtersList = new List<ExchangeFilter>(4);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        ParseUtility.EnsureObjectStartToken(ref reader);
                        int objectDepth = reader.CurrentDepth;
                        try
                        {
                            filtersList.Add(ParseUtility.ParseExchangeFilter(ref reader));
                        }
                        catch (JsonException jExc)
                        {
                            LogMessage(LogLevel.Warning, $"An exchange filter cannot be parsed: {jExc.Message}");
                            ParseUtility.SkipTillObjectEnd(ref reader, objectDepth);
                        }
                    }
                    ei.ExchangeFilters = filtersList;
                    break;
                case "symbols":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    List<SymbolInfo> symbolsList = new List<SymbolInfo>(expectedCount);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        symbolsList.Add(ParseSymbolInfo(ref reader));
                    }
                    ei.Symbols = symbolsList;
                    validator.RegisterProperty(3);
                    break;
                default:
                    LogMessage(LogLevel.Warning,
                        $"An unknown exchange info property \"{propName}\" was encountered while parsing the response.");
                    reader.Skip();
                    break;
            }
        }

        // Check whether all the essential properties were provided.
        if (!validator.IsComplete())
        {
            const string objName = "exchange info";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "timezone"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "server time"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "rate limits"),
                3 => ParseUtility.GenerateMissingPropertyException(objName, "symbols"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
            };
        }

        // Return the result.
        return ei;
    }

    private SymbolInfo ParseSymbolInfo(ref Utf8JsonReader reader)
    {
        ParseUtility.EnsureObjectStartToken(ref reader);

        SymbolInfo symbol = new SymbolInfo();
        ParseSchemaValidator validator = new ParseSchemaValidator(8);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
                throw new JsonException($"A value of the property \"{propName}\" was expected but \"{reader.TokenType}\" encountered.");
            switch (propName)
            {
                case "symbol":
                    symbol.Symbol = ParseUtility.GetNonEmptyString(ref reader, propName);
                    validator.RegisterProperty(0);
                    break;
                case "status":
                    symbol.Status = SpotUtility.ParseSymbolStatus(ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(1);
                    break;
                case "baseAsset":
                    symbol.BaseAsset = ParseUtility.GetNonEmptyString(ref reader, propName);
                    validator.RegisterProperty(2);
                    break;
                case "baseAssetPrecision":
                    symbol.BaseAssetPrecision = reader.GetByte();
                    validator.RegisterProperty(3);
                    break;
                case "quoteAsset":
                    symbol.QuoteAsset = ParseUtility.GetNonEmptyString(ref reader, propName);
                    validator.RegisterProperty(4);
                    break;
                case "quoteAssetPrecision":
                    symbol.QuoteAssetPrecision = reader.GetByte();
                    validator.RegisterProperty(5);
                    break;
                case "baseCommissionPrecision":
                    symbol.BaseComissionPrecision = reader.GetByte();
                    break;
                case "quoteCommissionPrecision":
                    symbol.QuoteComissionPrecision = reader.GetByte();
                    break;
                case "orderTypes":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    List<OrderType> orderTypesList = new List<OrderType>(8);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        orderTypesList.Add(SpotUtility.ParseOrderType(
                            ParseUtility.GetNonEmptyString(ref reader, propName)));
                    }
                    symbol.OrderTypes = orderTypesList;
                    validator.RegisterProperty(6);
                    break;
                case "icebergAllowed":
                    if (reader.GetBoolean()) symbol.Permissions |= SymbolPermissions.IcebergOrders;
                    break;
                case "ocoAllowed":
                    if (reader.GetBoolean()) symbol.Permissions |= SymbolPermissions.OCOOrders;
                    break;
                case "quoteOrderQtyMarketAllowed":
                    if (reader.GetBoolean()) symbol.Permissions |= SymbolPermissions.QuoteQuantityOrders;
                    break;
                case "allowTrailingStop":
                    if (reader.GetBoolean()) symbol.Permissions |= SymbolPermissions.TrailingStopOrders;
                    break;
                case "cancelReplaceAllowed":
                    if (reader.GetBoolean()) symbol.Permissions |= SymbolPermissions.OrderReplacing;
                    break;
                case "isSpotTradingAllowed":
                    if (reader.GetBoolean()) symbol.Permissions |= SymbolPermissions.SpotTrading;
                    break;
                case "isMarginTradingAllowed":
                    if (reader.GetBoolean()) symbol.Permissions |= SymbolPermissions.MarginTrading;
                    break;
                case "filters":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    List<SymbolFilter> filtersList = new List<SymbolFilter>(10);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        ParseUtility.EnsureObjectStartToken(ref reader);
                        int objectDepth = reader.CurrentDepth;
                        try
                        {
                            filtersList.Add(ParseUtility.ParseSymbolFilter(ref reader));
                        }
                        catch (JsonException jExc)
                        {
                            LogMessage(LogLevel.Warning, $"One of the symbol filters cannot be parsed: {jExc.Message}");
                            ParseUtility.SkipTillObjectEnd(ref reader, objectDepth);
                        }
                    }
                    symbol.Filters = filtersList;
                    validator.RegisterProperty(7);
                    break;
                case "defaultSelfTradePreventionMode":
                    symbol.DefaultSTPMode = SpotUtility.ParseSelfTradePreventionMode(
                        ParseUtility.GetNonEmptyString(ref reader, propName));
                    break;
                case "allowedSelfTradePreventionModes":
                    int arrayDepth = reader.CurrentDepth;
                    try
                    {
                        symbol.AllowedSTPModes = ParseSTPModeList(ref reader);
                    }
                    catch (JsonException jExc)
                    {
                        LogMessage(LogLevel.Warning, $"A symbol's allowed STP modes cannot be parsed: {jExc.Message}");
                        ParseUtility.SkipTillArrayEnd(ref reader, arrayDepth);
                    }
                    break;
                case "permissions":
                    // The property is not stored.
                    reader.Skip();
                    break;
                case "quotePrecision":
                    // The property is obsolete.
                    reader.Skip();
                    break;
                default:
                    LogMessage(
                        LogLevel.Warning,
                        $"An unknown symbol property \"{propName}\" was encountered.");
                    reader.Skip();
                    break;
            }
        }

        // Check whether all the essential properties were provided.
        if (!validator.IsComplete())
        {
            const string objName = "symbol info";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "status"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "base asset"),
                3 => ParseUtility.GenerateMissingPropertyException(objName, "base asset precision"),
                4 => ParseUtility.GenerateMissingPropertyException(objName, "quote asset"),
                5 => ParseUtility.GenerateMissingPropertyException(objName, "quote asset precision"),
                6 => ParseUtility.GenerateMissingPropertyException(objName, "order types"),
                7 => ParseUtility.GenerateMissingPropertyException(objName, "filters"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
            };
        }

        // Return the result.
        return symbol;
    }

    private List<SelfTradePreventionMode> ParseSTPModeList(ref Utf8JsonReader reader)
    {
        ParseUtility.EnsureArrayStartToken(ref reader);
        List<SelfTradePreventionMode> resultList = new List<SelfTradePreventionMode>(
            Enum.GetValues(typeof(SelfTradePreventionMode)).Length);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            resultList.Add(SpotUtility.ParseSelfTradePreventionMode(
                ParseUtility.GetNonEmptyString(ref reader, string.Empty)));
        }

        return resultList;
    }

    // Get products info.
    /// <summary>
    /// Creates a deferred products information query.
    /// <para>The products info includes traded assets' full names and circulating supply (where applicable).</para>
    /// <para>The query does not consume any limits because it's transfered through a public non-API endpoint.</para>
    /// </summary>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<Dictionary<string, Product>> PrepareGetProductsInfo()
    {
        ThrowIfNotRunning();

        QueryParams queryParams = new QueryParams(
            method: HttpMethod.GET,
            baseEndpoint: "https://www.binance.com",
            relativeEndpoint: "/exchange-api/v2/public/asset-service/product/get-products");
        return new DeferredQuery<Dictionary<string, Product>>(queryParams, ExecuteQueryAsync, ParseProductsInfoList);
    }

    /// <summary>
    /// Requests products information, including traded assets' full names and circulating supply (where applicable).
    /// </summary>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="QueryException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task<Dictionary<string, Product>> GetProductsInfoAsync(CancellationToken ct = default)
    {
        using (IDeferredQuery<Dictionary<string, Product>> query = PrepareGetProductsInfo())
        {
            return query.ExecuteAsync(ct);
        }
    }

    public Dictionary<string, Product> ParseProductsInfoList(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);
        Dictionary<string, Product> productsDict = new Dictionary<string, Product>(ExpectedSymbolsCount);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
                throw new JsonException($"A value of the property \"{propName}\" was expected but \"{reader.TokenType}\" encountered.");

            switch (propName)
            {
                case "code":
                    break;
                case "message":
                    break;
                case "messageDetail":
                    break;
                case "data":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        Product product = ParseProductInfo(ref reader);
                        if (product.Symbol != null)
                            productsDict.Add(product.Symbol, product);
                    }
                    break;
                case "success":
                    break;
                default:
                    LogMessage(LogLevel.Warning,
                        $"An unknown products info property \"{propName}\" was encountered in the response.");
                    reader.Skip();
                    break;
            }
        }

        return productsDict;
    }

    private Product ParseProductInfo(ref Utf8JsonReader reader)
    {
        ParseUtility.EnsureObjectStartToken(ref reader);
        Product product = Product.Undefined;

        ParseUtility.ReadExactPropertyName(ref reader, "s");
        product.Symbol = ParseUtility.ReadNonEmptyString(ref reader, "s");

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
                throw new JsonException($"A value of the property \"{propName}\" was expected but \"{reader.TokenType}\" encountered.");
            switch (propName)
            {
                case "st":
                    product.Status = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "b":
                    product.BaseAsset = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "q":
                    product.QuoteAsset = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "ba":
                    break;
                case "qa":
                    break;
                case "i":
                    break;
                case "ts":
                    break;
                case "an":
                    product.BaseAssetName = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "qn":
                    product.QuoteAssetName = ParseUtility.GetNonEmptyString(ref reader, propName);
                    break;
                case "o":
                    ParseUtility.ParseDouble(propName, reader.GetString(), out product.Open);
                    break;
                case "h":
                    ParseUtility.ParseDouble(propName, reader.GetString(), out product.High);
                    break;
                case "l":
                    ParseUtility.ParseDouble(propName, reader.GetString(), out product.Low);
                    break;
                case "c":
                    ParseUtility.ParseDouble(propName, reader.GetString(), out product.Close);
                    break;
                case "v":
                    ParseUtility.ParseDouble(propName, reader.GetString(), out product.BaseVolume);
                    break;
                case "qv":
                    ParseUtility.ParseDouble(propName, reader.GetString(), out product.QuoteVolume);
                    break;
                case "y":
                    break;
                case "as":
                    break;
                case "pm":
                    break;
                case "pn":
                    break;
                case "cs":
                    if (reader.TokenType != JsonTokenType.Null)
                    {
                        product.CirculatingSupply = reader.GetInt64();
                    }
                    break;
                case "tags":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        string? tag = reader.GetString();
                        if (!string.IsNullOrEmpty(tag))
                        {
                            product.Tags.Add(tag);
                        }
                    }
                    break;
                case "pom":
                    break;
                case "pomt":
                    break;
                case "lc":
                    break;
                case "g":
                    break;
                case "r":
                    break;
                case "hd":
                    break;
                case "rb":
                    break;
                case "etf":
                    break;
                case "sd":
                    break;
                default:
                    LogMessage(LogLevel.Warning,
                        $"An unknown product property \"{propName}\" was encountered " +
                        $"while parsing the product \"{product.Symbol}\".");
                    break;
            }
        }

        return product;
    }

    // Get raw historical trades.
    /// <summary>
    /// Prepares a query for older market trades.
    /// </summary>
    /// <param name="symbol">A symbol to get trades for.</param>
    /// <param name="limit">A maximum number of trades to fetch. The maximum value is 1000. The default value is 500.</param>
    /// <param name="fromId">The ID of a trade to fetch from. If not specified, the recent trades are fetched.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<List<Trade>> PrepareGetOldTrades(string symbol, int? limit = null, long? fromId = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 10),
        };

        QueryBuilder qs = new QueryBuilder(56);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        if (limit != null)
            qs.AddParameter("limit", Math.Clamp(limit.Value, 1, 1000));
        if (fromId != null)
            qs.AddParameter("fromId", fromId.Value);

        return new DeferredQuery<List<Trade>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetOldTradesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseTradesList,
            parseArgs: limit ?? 500,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets older market trades asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="QueryException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task<List<Trade>> GetOldTradesAsync(
        string symbol, int? limit = null, long? fromId = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<Trade>> query = PrepareGetOldTrades(symbol, limit, fromId))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<Trade> ParseTradesList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<Trade> results = new List<Trade>(parseArgs is int expectedCount ? expectedCount : 500);

        ParseSchemaValidator validator = new ParseSchemaValidator(6);
        long id = default;
        decimal price = default, quantity = default, quoteQty = default;
        DateTime time = DateTime.MinValue;
        bool wasBuyerMaker = default;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ParseUtility.EnsureObjectStartToken(ref reader);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                if (!reader.Read())
                {
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                }
                switch (propName)
                {
                    case "id":
                        id = reader.GetInt64();
                        validator.RegisterProperty(0);
                        break;
                    case "price":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out price);
                        validator.RegisterProperty(1);
                        break;
                    case "qty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out quantity);
                        validator.RegisterProperty(2);
                        break;
                    case "quoteQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out quoteQty);
                        validator.RegisterProperty(3);
                        break;
                    case "time":
                        time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(4);
                        break;
                    case "isBuyerMaker":
                        wasBuyerMaker = reader.GetBoolean();
                        validator.RegisterProperty(5);
                        break;
                    case "isBestMatch":
                        // Not stored.
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "trade";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => ParseUtility.GenerateMissingPropertyException(objName, "ID"),
                    1 => ParseUtility.GenerateMissingPropertyException(objName, "price"),
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "quantity"),
                    3 => ParseUtility.GenerateMissingPropertyException(objName, "quote quantity"),
                    4 => ParseUtility.GenerateMissingPropertyException(objName, "time"),
                    5 => ParseUtility.GenerateMissingPropertyException(objName, "was buyer maker"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
                };
            }

            // Add the trade to the results list.
            results.Add(new Trade(id, price, quantity, quoteQty, time, wasBuyerMaker));
            validator.Reset();
        }

        return results;
    }

    // Get aggregated historical trades.
    /// <summary>
    /// Prepares a query for compressed, aggregate trades for the specified time period.
    /// <para>Trades that fill at the time, from the same order, with the same price will have the quantity aggregated.</para>
    /// <para>If none of <paramref name="startTime"/> and <paramref name="endTime"/> are specified,
    /// the most recent aggregate trades will be fetched.</para>
    /// </summary>
    /// <param name="symbol">A symbol to get trades for.</param>
    /// <param name="limit">
    /// The maximum number of trades to fetch.
    /// <para>The maximum value is <see cref="MaxTradesQueryLimit"/> (1000).</para>
    /// <para>If not specified, the default value <see cref="DefaultTradesQueryLimit"/> (500) is used.</para>
    /// </param>
    /// <param name="startTime">Time to fetch aggregate trades from (inclusive).</param>
    /// <param name="endTime">Time to fetch aggregate trades until (inclusive).</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<List<AggregateTrade>> PrepareGetAggregateTrades(
        string symbol,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxTradesQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 2),
        };

        QueryBuilder qs = new QueryBuilder(75);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);

        return new DeferredQuery<List<AggregateTrade>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAggregateTradesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseAggregateTradesList,
            parseArgs: limit ?? DefaultTradesQueryLimit,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets compressed, aggregate trades for the specified time period, asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="QueryException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task<List<AggregateTrade>> GetAggregateTradesAsync(
        string symbol,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<AggregateTrade>> query = PrepareGetAggregateTrades(symbol, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for compressed, aggregate trades starting from the specified trade.
    /// <para>Trades that fill at the time, from the same order, with the same price will have the quantity aggregated.</para>
    /// </summary>
    /// <param name="symbol">A symbol to get trades for.</param>
    /// <param name="limit">
    /// The maximum number of trades to fetch.
    /// <para>The maximum value is <see cref="MaxTradesQueryLimit"/> (1000).</para>
    /// <para>If not specified, the default value <see cref="DefaultTradesQueryLimit"/> (500) is used.</para>
    /// </param>
    /// <param name="fromId">The ID of a trade to fetch from (inclusive).</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<List<AggregateTrade>> PrepareGetAggregateTrades(
        string symbol, long fromId, int? limit = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxTradesQueryLimit);

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
        };

        QueryBuilder qs = new QueryBuilder(112);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        qs.AddParameter("fromId", fromId);
        if (limit != null)
            qs.AddParameter("limit", limit.Value);

        return new DeferredQuery<List<AggregateTrade>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAggregateTradesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseAggregateTradesList,
            parseArgs: limit ?? DefaultTradesQueryLimit,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets compressed, aggregate trades starting from the specified trade, asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="QueryException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task<List<AggregateTrade>> GetAggregateTradesAsync(
        string symbol, long fromId, int? limit = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<AggregateTrade>> query = PrepareGetAggregateTrades(symbol, fromId, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<AggregateTrade> ParseAggregateTradesList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        List<AggregateTrade> results = new List<AggregateTrade>(
            parseArgs is int expectedCount ? expectedCount : DefaultTradesQueryLimit);

        ParseUtility.ReadArrayStart(ref reader);

        ParseSchemaValidator validator = new ParseSchemaValidator(7);
        long id = default, firstTradeId = default, lastTradeId = default;
        decimal price = default, quantity = default;
        DateTime timestamp = DateTime.MinValue;
        bool wasBuyerMaker = default;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ParseUtility.EnsureObjectStartToken(ref reader);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                if (!reader.Read())
                {
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                }

                switch (propName)
                {
                    case "a":
                        id = reader.GetInt64();
                        validator.RegisterProperty(0);
                        break;
                    case "p":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out price);
                        validator.RegisterProperty(1);
                        break;
                    case "q":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out quantity);
                        validator.RegisterProperty(2);
                        break;
                    case "f":
                        firstTradeId = reader.GetInt64();
                        validator.RegisterProperty(3);
                        break;
                    case "l":
                        lastTradeId = reader.GetInt64();
                        validator.RegisterProperty(4);
                        break;
                    case "T":
                        timestamp = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(5);
                        break;
                    case "m":
                        wasBuyerMaker = reader.GetBoolean();
                        validator.RegisterProperty(6);
                        break;
                    case "M":
                        // Is best match?. This property is not stored.
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether the trade is marked as invalid by the API.
            if (firstTradeId == -1 && lastTradeId == -1 && price == 0.0m && quantity == 0.0m)
            {
                continue;
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "aggregate trade";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => ParseUtility.GenerateMissingPropertyException(objName, "ID"),
                    1 => ParseUtility.GenerateMissingPropertyException(objName, "price"),
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "quantity"),
                    3 => ParseUtility.GenerateMissingPropertyException(objName, "first trade ID"),
                    4 => ParseUtility.GenerateMissingPropertyException(objName, "last trade ID"),
                    5 => ParseUtility.GenerateMissingPropertyException(objName, "timestamp"),
                    6 => ParseUtility.GenerateMissingPropertyException(objName, "was buyer maker"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
                };
            }

            // Add the trade to the results list.
            results.Add(new AggregateTrade(id, price, quantity, firstTradeId, lastTradeId, timestamp, wasBuyerMaker));
            validator.Reset();
        }

        return results;
    }

    // Get candlestick data.
    /// <summary>
    /// Prepares a query for kline/candlestick bars for the specified symbol.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// </summary>
    /// <param name="symbol">A symbol to get candlestick data for.</param>
    /// <param name="limit">
    /// The maximum number of candlesticks to fetch (optional).
    /// <para>The maximum value is <see cref="MaxKlinesQueryLimit"/> (1000).</para>
    /// <para>If not specified, the default value <see cref="DefaultKlinesQueryLimit"/> (500) is used.</para></param>
    /// <param name="startTime">The time to fetch candlesticks from, based on their opening time (inclusive, optional).</param>
    /// <param name="endTime">The time to fetch candlesticks prior to, based on their opening time (inclusive, optional).</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<List<Candlestick>> PrepareGetCandlestickData(
        string symbol,
        KlineInterval interval,
        int? limit = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxKlinesQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 2),
        };

        QueryBuilder qs = new QueryBuilder(109);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        qs.AddParameter("interval", SpotUtility.Format(interval));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));

        return new DeferredQuery<List<Candlestick>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetCandlestickDataEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseCandlestickList,
            parseArgs: limit ?? DefaultKlinesQueryLimit,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets kline/candlestick bars for the specified symbol asynchronously.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="QueryException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task<List<Candlestick>> GetCandlestickDataAsync(
        string symbol,
        KlineInterval interval,
        int? limit = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<Candlestick>> query =
            PrepareGetCandlestickData(symbol, interval, limit, startTime, endTime))
        {
            return query.ExecuteAsync(ct);
        }
    }

    [SuppressMessage("Style", "IDE0018:Inline variable declaration", Justification = "Explicit declaration is preferred")]
    private List<Candlestick> ParseCandlestickList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        List<Candlestick> results = new List<Candlestick>(
            parseArgs is int expectedCount ? expectedCount : DefaultKlinesQueryLimit);
        DateTime openTime, closeTime;
        decimal o, h, l, c, bv, qv, tbv, tqv;
        uint numOfTrades;

        ParseUtility.ReadArrayStart(ref reader);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ParseUtility.EnsureArrayStartToken(ref reader);

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"A candlestick open time value was expected but \"{reader.TokenType}\" encountered.");
            openTime = CommonUtility.ConvertToDateTime(reader.GetInt64());

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick open price value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("open", reader.GetString(), out o);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick high price value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("high", reader.GetString(), out h);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick low price value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("low", reader.GetString(), out l);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick close price value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("close", reader.GetString(), out c);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick base asset volume value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("base asset volume", reader.GetString(), out bv);

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"A candlestick close time value was expected but \"{reader.TokenType}\" encountered.");
            closeTime = CommonUtility.ConvertToDateTime(reader.GetInt64());

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick quote asset volume value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("quote asset volume", reader.GetString(), out qv);

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"A candlestick trades number value was expected but \"{reader.TokenType}\" encountered.");
            numOfTrades = reader.GetUInt32();

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick taker buy base asset volume value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("taker buy base asset volume", reader.GetString(), out tbv);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick taker buy quote asset volume value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("taker buy base quote volume", reader.GetString(), out tqv);

            if (!reader.Read())
                throw new JsonException($"The last property value was expected but \"{reader.TokenType}\" encountered.");

            ParseUtility.ReadArrayEnd(ref reader);
            results.Add(new Candlestick(openTime, closeTime, o, h, l, c, bv, qv, numOfTrades, tbv, tqv));
        }

        // Add the candlestick to the results list.
        return results;
    }

    // Get price tickers.
    /// <summary>
    /// Prepares a query for the specified symbols' latest prices.
    /// </summary>
    /// <param name="symbols">The symbols to get latest prices for, or null to get prices for all symbols.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    public IDeferredQuery<List<PriceTick>> PrepareGetSymbolPriceTicker(params string[] symbols)
    {
        ThrowIfNotRunning();
        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), symbols != null && symbols.Length == 1 ? 2u : 4u),
        };

        QueryBuilder? qs = null;
        if (symbols != null && symbols.Length != 0)
        {
            int emptyIdx = CommonUtility.IndexOfEmpty(symbols);
            if (emptyIdx != -1)
            {
                throw new ArgumentException(
                    $"The item {emptyIdx} in the specified array is either null or empty.",
                    nameof(symbols));
            }

            if (symbols.Length == 1)
            {
                qs = new QueryBuilder(8 + symbols[0].Length);
                qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbols[0]));
            }
            else
            {
                qs = new QueryBuilder(11 + 11 * symbols.Length);
                qs.AddParameter("symbols", symbols.Select<string, string>(CommonUtility.NormalizeSymbol));
            }
        }

        return new DeferredQuery<List<PriceTick>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetSymbolPriceTickerEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParsePriceTickList,
            parseArgs: symbols != null ? symbols.Length : null,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets latest prices for the specified symbols asynchronously.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="QueryException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task<List<PriceTick>> GetSymbolPriceTickerAsync(CancellationToken ct, params string[] symbols)
    {
        using (IDeferredQuery<List<PriceTick>> query = PrepareGetSymbolPriceTicker(symbols))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<PriceTick> ParsePriceTickList(byte[] data, object? parseArgs = null)
    {
        List<PriceTick> results = new List<PriceTick>(
            parseArgs is int expectedCount ? expectedCount : ExpectedSymbolsCount);
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        if (!reader.Read())
        {
            throw new JsonException(
                $"An array's or object's start was expected, " +
                $"but the end of the data was reached.");
        }

        switch (reader.TokenType)
        {
            case JsonTokenType.StartArray:
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    // Add the price tick to the results list.
                    results.Add(ParseSinglePriceTick(ref reader));
                }
                return results;

            case JsonTokenType.StartObject:
                results.Add(ParseSinglePriceTick(ref reader));
                return results;

            default:
                throw new JsonException(
                    $"An array's or object's start was expected, " +
                    $"but {reader.TokenType} was encountered.");
        }
    }

    private PriceTick ParseSinglePriceTick(ref Utf8JsonReader reader)
    {
        ParseUtility.EnsureObjectStartToken(ref reader);

        const string objName = "price tick";
        string? s = null;
        decimal p = -1.0m;

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
                    s = reader.GetString();
                    break;
                case "price":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out p);
                    break;
                default:
                    LogMessage(LogLevel.Warning, $"An unknown {objName} property \"{propName}\" was encountered.");
                    reader.Skip();
                    break;
            }
        }

        // Check whether all essential properties were provided.
        if (s == null)
        {
            throw ParseUtility.GenerateMissingPropertyException(objName, "symbol");
        }
        else if (p == -1.0m)
        {
            throw ParseUtility.GenerateMissingPropertyException(objName, "price");
        }

        // Add the price tick to the results list.
        return new PriceTick(s, p);
    }

    #endregion
}
