﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Utility;
using Oakbranch.Binance.Models;
using Oakbranch.Binance.Models.Futures;
using Oakbranch.Binance.Models.Filters.Exchange;
using Oakbranch.Binance.Models.Filters.Symbol;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Clients;

/// <summary>
/// Encapsulates functions for accessing the market data endpoints of the Binance USD-Margined Futures API.
/// </summary>
public class FuturesCMMarketApiClient : FuturesCMClientBase
{
    #region Constants

    // Common.
    /// <summary>
    /// Defines the expected number of futures contract symbols reported by the server.
    /// <para>Note: there were 62 symbols as of 01.05.2023</para>
    /// </summary>
    public const int ExpectedSymbolsCount = 80;

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
    public const int MaxKlinesQueryLimit = 1500;
    /// <summary>
    /// The default number of items that can be fetched in a single candlesticks query.
    /// </summary>
    public const int DefaultKlinesQueryLimit = 500;
    /// <summary>
    /// The maximum number of items that can be fetched in a single funding rate history query.
    /// </summary>
    public const int MaxFundingHistoryQueryLimit = 1000;
    /// <summary>
    /// The default number of items that can be fetched in a single funding rate history query.
    /// </summary>
    public const int DefaultFundingHistoryQueryLimit = 100;
    /// <summary>
    /// The maximum number of items that can be fetched in a single market stats query (open interest, LS ratio etc).
    /// </summary>
    public const int MaxMarketStatsQueryLimit = 500;
    /// <summary>
    /// The default number of items that can be fetched in a single market stats query (open interest, LS ratio etc).
    /// </summary>
    public const int DefaultMarketStatsQueryLimit = 30;
    /// <summary>
    /// Defines the maximum allowed period to query index price history within (in ticks).
    /// </summary>
    public const long MaxIndexPriceLookupInterval = 200 * TimeSpan.TicksPerDay;

    // Endpoints.
    private const string MarketStatsDiscriminativeEndpoint = "/futures/data";
    private const string GetServerTimeEndpoint = "/dapi/v1/time";
    private const string GetOldTradesEndpoint = "/dapi/v1/historicalTrades";
    private const string GetAggregateTradesEndpoint = "/dapi/v1/aggTrades";
    private const string GetSymbolPriceKlinesEndpoint = "/dapi/v1/klines";
    private const string GetContractPriceKlinesEndpoint = "/dapi/v1/continuousKlines";
    private const string GetIndexPriceKlinesEndpoint = "/dapi/v1/indexPriceKlines";
    private const string GetMarkPriceKlinesEndpoint = "/dapi/v1/markPriceKlines";
    private const string GetPremiumIndexKlinesEndpoint = "/dapi/v1/premiumIndexKlines";
    private const string GetPremiumInfoEndpoint = "/dapi/v1/premiumIndex";
    private const string GetFundingRateHistoryEndpoint = "/dapi/v1/fundingRate";
    private const string GetFundingRateInfoEndpoint = "/dapi/v1/fundingInfo";
    private const string GetCurrentOpenInterestEndpoint = "/dapi/v1/openInterest";
    private const string GetOpenInterestHistoryEndpoint = "/futures/data/openInterestHist";
    private const string GetTopAccountLSRatioEndpoint = "/futures/data/topLongShortAccountRatio";
    private const string GetTopPositionLSRatioEndpoint = "/futures/data/topLongShortPositionRatio";
    private const string GetAllAccountLSRatioEndpoint = "/futures/data/globalLongShortAccountRatio";
    private const string GetTakerBuySellVolumeEndpoint = "/futures/data/takerBuySellVol";

    #endregion

    #region Instance props & fields

    private readonly ReadOnlyDictionary<string, int> _dummyHeadersLimitsMap;

    #endregion

    #region Instance constructors

    public FuturesCMMarketApiClient(
        IApiConnector connector,
        IRateLimitsRegistry limitsRegistry,
        ILogger<FuturesCMMarketApiClient>? logger = null)
        : base(connector, limitsRegistry, logger)
    {
        _dummyHeadersLimitsMap = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(0));

        if (!Connector.IsLimitMetricsMapRegistered(MarketStatsDiscriminativeEndpoint))
        {
            Connector.SetLimitMetricsMap(MarketStatsDiscriminativeEndpoint, Array.Empty<string>());
        }
    }

    #endregion

    #region Instance methods

    // Server time.
    /// <summary>
    /// Creates a deferred query to get the current server time.
    /// </summary>
    /// <returns></returns>
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
            throw new JsonException($"The server time property was expected but \"{propName}\" encountered.");

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException($"The server time value (number) was expected but \"{reader.TokenType}\" encountered.");
        long ms = reader.GetInt64();

        ParseUtility.ReadObjectEnd(ref reader);

        return CommonUtility.ConvertToDateTime(ms);
    }

    // Exchange info.
    /// <summary>
    /// Creates a deferred query for USD-M futures exchange information.
    /// </summary>
    public IDeferredQuery<FuturesExchangeInfo> PrepareGetExchangeInfo()
    {
        ThrowIfNotRunning();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
        };

        return new DeferredQuery<FuturesExchangeInfo>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetExchangeInfoEndpoint, null, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseExchangeInfoResponse,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets USD-M futures exchange information asynchronously.
    /// </summary>
    public Task<FuturesExchangeInfo> GetExchangeInfoAsync(CancellationToken ct = default)
    {
        using (IDeferredQuery<FuturesExchangeInfo> query = PrepareGetExchangeInfo())
        {
            return query.ExecuteAsync(ct);
        }
    }

    private FuturesExchangeInfo ParseExchangeInfoResponse(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        FuturesExchangeInfo result = new FuturesExchangeInfo();
        ParseSchemaValidator validator = new ParseSchemaValidator(3);

        ParseUtility.ReadObjectStart(ref reader);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
            {
                throw ParseUtility.GenerateNoPropertyValueException(propName);
            }

            switch (propName)
            {
                case "timezone":
                    string timezone = ParseUtility.GetNonEmptyString(ref reader, propName);
                    result.Timezone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                    validator.RegisterProperty(0);
                    break;
                case "rateLimits":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    List<RateLimiter> limits = new List<RateLimiter>(6);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        limits.Add(ParseRateLimiter(ref reader));
                    }
                    result.RateLimits = limits;
                    validator.RegisterProperty(1);
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
                    result.ExchangeFilters = filtersList;
                    break;
                case "symbols":
                    result.Symbols = ParseSymbolInfoList(ref reader);
                    validator.RegisterProperty(2);
                    break;
                case "serverTime":
                    // The property is not stored.
                    break;
                case "futuresType":
                    // The property is not stored.
                    reader.Skip();
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
                1 => ParseUtility.GenerateMissingPropertyException(objName, "rate limits"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "symbols"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
            };
        }

        // Return the result.
        return result;
    }

    private List<SymbolInfo> ParseSymbolInfoList(ref Utf8JsonReader reader)
    {
        List<SymbolInfo> resultList = new List<SymbolInfo>(ExpectedSymbolsCount);
        ParseSchemaValidator validator = new ParseSchemaValidator(21);

        ParseUtility.EnsureArrayStartToken(ref reader);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ParseUtility.EnsureObjectStartToken(ref reader);
            int objectDepth = reader.CurrentDepth;
            SymbolInfo symbol = new SymbolInfo();

            try
            {
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
                            symbol.Symbol = ParseUtility.GetNonEmptyString(ref reader, propName);
                            validator.RegisterProperty(0);
                            break;
                        case "pair":
                            symbol.Pair = ParseUtility.GetNonEmptyString(ref reader, propName);
                            validator.RegisterProperty(1);
                            break;
                        case "contractType":
                            symbol.ContractType = FuturesUtility.ParseContractType(
                                ParseUtility.GetNonEmptyString(ref reader, propName));
                            validator.RegisterProperty(2);
                            break;
                        case "deliveryDate":
                            symbol.DeliveryDate = CommonUtility.ConvertToDateTime(reader.GetInt64());
                            validator.RegisterProperty(3);
                            break;
                        case "onboardDate":
                            symbol.OnboardDate = CommonUtility.ConvertToDateTime(reader.GetInt64());
                            validator.RegisterProperty(4);
                            break;
                        case "contractStatus":
                            symbol.Status = FuturesUtility.ParseContractStatus(
                                ParseUtility.GetNonEmptyString(ref reader, propName));
                            validator.RegisterProperty(5);
                            break;
                        case "contractSize":
                            symbol.QuoteSize = reader.GetUInt32();
                            validator.RegisterProperty(6);
                            break;
                        case "baseAsset":
                            symbol.BaseAsset = ParseUtility.GetNonEmptyString(ref reader, propName);
                            validator.RegisterProperty(7);
                            break;
                        case "quoteAsset":
                            symbol.QuoteAsset = ParseUtility.GetNonEmptyString(ref reader, propName);
                            validator.RegisterProperty(8);
                            break;
                        case "marginAsset":
                            symbol.MarginAsset = ParseUtility.GetNonEmptyString(ref reader, propName);
                            validator.RegisterProperty(9);
                            break;
                        case "pricePrecision":
                            symbol.PricePrecision = reader.GetByte();
                            validator.RegisterProperty(10);
                            break;
                        case "quantityPrecision":
                            symbol.QuantityPrecision = reader.GetByte();
                            validator.RegisterProperty(11);
                            break;
                        case "baseAssetPrecision":
                            symbol.BaseAssetPrecision = reader.GetByte();
                            validator.RegisterProperty(12);
                            break;
                        case "quotePrecision":
                            symbol.QuoteAssetPrecision = reader.GetByte();
                            validator.RegisterProperty(13);
                            break;
                        case "underlyingType":
                            symbol.UnderlyingType = ParseUtility.GetNonEmptyString(ref reader, propName);
                            break;
                        case "underlyingSubType":
                            ParseUtility.EnsureArrayStartToken(ref reader);
                            List<string> subtypes = new List<string>();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                subtypes.Add(ParseUtility.GetNonEmptyString(ref reader, propName));
                            }
                            symbol.UnderlyingSubtypes = subtypes;
                            break;
                        case "triggerProtect":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out symbol.ProtectionThreshold);
                            validator.RegisterProperty(14);
                            break;
                        case "filters":
                            ParseUtility.EnsureArrayStartToken(ref reader);
                            List<SymbolFilter> filtersList = new List<SymbolFilter>(10);
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                ParseUtility.EnsureObjectStartToken(ref reader);
                                int filterDepth = reader.CurrentDepth;
                                try
                                {
                                    filtersList.Add(ParseUtility.ParseSymbolFilter(ref reader));
                                }
                                catch (JsonException jExc)
                                {
                                    LogMessage(LogLevel.Warning, $"One of the symbol filters cannot be parsed: {jExc.Message}");
                                    ParseUtility.SkipTillObjectEnd(ref reader, filterDepth);
                                }
                            }
                            symbol.Filters = filtersList;
                            validator.RegisterProperty(15);
                            break;
                        case "orderTypes":
                            ParseUtility.EnsureArrayStartToken(ref reader);
                            List<OrderType> orderTypesList = new List<OrderType>(8);
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                orderTypesList.Add(FuturesUtility.ParseOrderType(
                                    ParseUtility.GetNonEmptyString(ref reader, propName)));
                            }
                            symbol.OrderTypes = orderTypesList;
                            validator.RegisterProperty(16);
                            break;
                        case "timeInForce":
                            ParseUtility.EnsureArrayStartToken(ref reader);
                            List<TimeInForce> tifList = new List<TimeInForce>(4);
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                tifList.Add(FuturesUtility.ParseTimeInForce(ParseUtility.GetNonEmptyString(ref reader, propName)));
                            }
                            symbol.TimeInForceRules = tifList;
                            validator.RegisterProperty(17);
                            break;
                        case "liquidationFee":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out symbol.LiquidationFee);
                            validator.RegisterProperty(18);
                            break;
                        case "marketTakeBound":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out symbol.MarketTakeBound);
                            validator.RegisterProperty(19);
                            break;
                        case "maxMoveOrderLimit":
                            symbol.MaxMoveOrderLimit = reader.GetInt32();
                            validator.RegisterProperty(20);
                            break;
                        case "maintMarginPercent":
                        case "requiredMarginPercent":
                        case "equalQtyPrecision":
                            // The property's designation is unknown, and it's not stored.
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
                        1 => ParseUtility.GenerateMissingPropertyException(objName, "pair"),
                        2 => ParseUtility.GenerateMissingPropertyException(objName, "contract type"),
                        3 => ParseUtility.GenerateMissingPropertyException(objName, "delivery date"),
                        4 => ParseUtility.GenerateMissingPropertyException(objName, "onboard date"),
                        5 => ParseUtility.GenerateMissingPropertyException(objName, "contract status"),
                        6 => ParseUtility.GenerateMissingPropertyException(objName, "contract size"),
                        7 => ParseUtility.GenerateMissingPropertyException(objName, "base asset"),
                        8 => ParseUtility.GenerateMissingPropertyException(objName, "quote asset"),
                        9 => ParseUtility.GenerateMissingPropertyException(objName, "margin asset"),
                        10 => ParseUtility.GenerateMissingPropertyException(objName, "price precision"),
                        11 => ParseUtility.GenerateMissingPropertyException(objName, "quantity precision"),
                        12 => ParseUtility.GenerateMissingPropertyException(objName, "base asset precision"),
                        13 => ParseUtility.GenerateMissingPropertyException(objName, "quote asset precision"),
                        14 => ParseUtility.GenerateMissingPropertyException(objName, "trigger protection threshold"),
                        15 => ParseUtility.GenerateMissingPropertyException(objName, "filters"),
                        16 => ParseUtility.GenerateMissingPropertyException(objName, "order types"),
                        17 => ParseUtility.GenerateMissingPropertyException(objName, "time-in-force rules"),
                        18 => ParseUtility.GenerateMissingPropertyException(objName, "liquidation fee"),
                        19 => ParseUtility.GenerateMissingPropertyException(objName, "market take bound"),
                        20 => ParseUtility.GenerateMissingPropertyException(objName, "max move order limit"),
                        _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
                    };
                }

                // Add the symbol info instance to the result list.
                resultList.Add(symbol);
            }
            catch (JsonException jExc)
            {
                LogMessage(LogLevel.Warning, $"The symbol info \"{symbol.Symbol}\" cannot be parsed: {jExc.Message}");
                ParseUtility.SkipTillObjectEnd(ref reader, objectDepth);
            }
            finally
            {
                validator.Reset();
            }
        }

        // Return the result.
        return resultList;
    }

    // Get old trades.
    /// <summary>
    /// Prepares a query for older market trades.
    /// </summary>
    /// <param name="symbol">The futures contract symbol to get trades for.</param>
    /// <param name="limit">
    /// The maximum number of trades to fetch.
    /// <para>The maximum value is <see cref="MaxTradesQueryLimit"/> (1000).</para>
    /// <para>If not specified, the default value <see cref="DefaultTradesQueryLimit"/> (500) is used.</para>
    /// </param>
    /// <param name="fromId">The ID of a trade to fetch from. If not specified, the recent trades are fetched.</param>
    public IDeferredQuery<List<Trade>> PrepareGetOldTrades(string symbol, int? limit = null, long? fromId = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxTradesQueryLimit);

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 20),
        };

        QueryBuilder qs = new QueryBuilder(56);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);
        if (fromId != null)
            qs.AddParameter("fromId", fromId.Value);

        return new DeferredQuery<List<Trade>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetOldTradesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseTradesList,
            parseArgs: limit,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets older market trades.
    /// </summary>
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
        List<Trade> results = new List<Trade>(parseArgs is int expectedCount ? expectedCount : DefaultTradesQueryLimit);

        ParseUtility.ReadArrayStart(ref reader);
        ParseSchemaValidator validator = new ParseSchemaValidator(6);
        long id = default;
        decimal price = default, baseQty = default, quoteQty = default;
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
                    case "baseQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out baseQty);
                        validator.RegisterProperty(2);
                        break;
                    case "qty":
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
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "base quantity"),
                    3 => ParseUtility.GenerateMissingPropertyException(objName, "quote quantity"),
                    4 => ParseUtility.GenerateMissingPropertyException(objName, "time"),
                    5 => ParseUtility.GenerateMissingPropertyException(objName, "was buyer maker"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
                };
            }

            // Add the trade to the results list.
            results.Add(new Trade(id, price, baseQty, quoteQty, time, wasBuyerMaker));
            validator.Reset();
        }

        return results;
    }

    // Get aggregated historical trades.
    /// <summary>
    /// Prepares a query for compressed aggregate trades.
    /// <para>Market trades that fill in 100ms with the same price and the same taking side will have the quantity aggregated.</para>
    /// <para>If <paramref name="fromId"/> is not specified the most recent aggregate trades will be fetched.</para>
    /// </summary>
    /// <param name="symbol">The futures contract symbol to get trades for.</param>
    /// <param name="fromId">The ID of the trade to fetch from (inclusive).</param>
    /// <param name="limit">
    /// The maximum number of trades to fetch.
    /// <para>The maximum value is <see cref="MaxTradesQueryLimit"/> (1000).</para>
    /// <para>If not specified, the default value <see cref="DefaultTradesQueryLimit"/> (500) is used.</para>
    /// </param>
    public IDeferredQuery<List<AggregateTrade>> PrepareGetAggregateTrades(
        string symbol, long fromId, int? limit = null)
    {
        return PrepareGetAggregateTrades(symbol, fromId, null, null, limit);
    }

    /// <summary>
    /// Gets compressed aggregate trades asynchronously.
    /// <para>Market trades that fill in 100ms with the same price and the same taking side will have the quantity aggregated.</para>
    /// <para>If <paramref name="fromId"/> is not specified the most recent aggregate trades will be fetched.</para>
    /// </summary>
    public Task<List<AggregateTrade>> GetAggregateTradesAsync(
        string symbol, long fromId, int? limit = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<AggregateTrade>> query = PrepareGetAggregateTrades(symbol, fromId, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for compressed aggregate trades.
    /// <para>Market trades that fill in 100ms with the same price and the same taking side will have the quantity aggregated.</para>
    /// </summary>
    /// <param name="symbol">The futures contract symbol to get trades for.</param>
    /// <param name="startTime">The time to fetch aggregate trades from (inclusive).
    /// If <paramref name="endTime"/> is specified too, the interval between these values must be less than one hour.</param>
    /// <param name="endTime">The time to fetch aggregate trades until (inclusive).
    /// The interval between start time and end time must be less than one hour.</param>
    /// <param name="limit">
    /// The maximum number of trades to fetch.
    /// <para>The maximum value is <see cref="MaxTradesQueryLimit"/> (1000).</para>
    /// <para>If not specified, the default value <see cref="DefaultTradesQueryLimit"/> (500) is used.</para>
    /// </param>
    public IDeferredQuery<List<AggregateTrade>> PrepareGetAggregateTrades(
        string symbol, DateTime? startTime, DateTime? endTime, int? limit = null)
    {
        return PrepareGetAggregateTrades(symbol, null, startTime, endTime, limit);
    }

    /// <summary>
    /// Gets compressed aggregate trades asynchronously.
    /// <para>Market trades that fill in 100ms with the same price and the same taking side will have the quantity aggregated.</para>
    /// </summary>
    public Task<List<AggregateTrade>> GetAggregateTradesAsync(
        string symbol, DateTime? startTime, DateTime? endTime, int? limit = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<AggregateTrade>> query = PrepareGetAggregateTrades(symbol, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private IDeferredQuery<List<AggregateTrade>> PrepareGetAggregateTrades(
        string symbol, long? fromId = null, DateTime? startTime = null, DateTime? endTime = null, int? limit = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxTradesQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 20),
        };

        QueryBuilder qs = new QueryBuilder(112);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));

        if (fromId != null) qs.AddParameter("fromId", fromId.Value);
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        if (limit != null)
            qs.AddParameter("limit", Math.Clamp(limit.Value, 1, 1000));

        return new DeferredQuery<List<AggregateTrade>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAggregateTradesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseAggregateTradesList,
            parseArgs: limit,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
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
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
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

    // Get candlestick data on symbol price.
    /// <summary>
    /// Prepares a query for kline/candlestick bars for the market price of the specified symbol.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    /// <param name="symbol">The futures contract symbol to get candlestick data for.</param>
    /// <param name="startTime">The time to fetch candlesticks from, based on their opening time (inclusive, optional).</param>
    /// <param name="endTime">The time to fetch candlesticks prior to, based on their opening time (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of candlesticks to fetch.
    /// <para>The maximum value is <see cref="MaxKlinesQueryLimit"/> (1500).</para>
    /// <para>If not specified, the default value <see cref="DefaultKlinesQueryLimit"/> (500) is used.</para>
    /// </param>
    public IDeferredQuery<List<Candlestick>> PrepareGetSymbolPriceKlines(
        string symbol,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxKlinesQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(109);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        qs.AddParameter("interval", FuturesUtility.Format(interval));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));

        return new DeferredQuery<List<Candlestick>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetSymbolPriceKlinesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseCompleteCandlestickList,
            parseArgs: limit ?? DefaultKlinesQueryLimit,
            weights: GetCandlestickQueryWeight(limit ?? DefaultKlinesQueryLimit),
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets kline/candlestick bars for the market price of the specified symbol, asynchronously.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    public Task<List<Candlestick>> GetSymbolPriceKlinesAsync(
        string symbol,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<Candlestick>> query =
            PrepareGetSymbolPriceKlines(symbol, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get candlestick data on specific contract's price.
    /// <summary>
    /// Prepares a query for continuous kline/candlestick bars for the market price of a specific contract.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    /// <param name="pair">The underlying trading pair of the futures contract to get candlestick data for.</param>
    /// <param name="contractType">The type of the futures contract to get candlestick data for.</param>
    /// <param name="startTime">The time to fetch candlesticks from, based on their opening time (inclusive, optional).</param>
    /// <param name="endTime">The time to fetch candlesticks prior to, based on their opening time (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of candlesticks to fetch.
    /// <para>The maximum value is <see cref="MaxKlinesQueryLimit"/> (1500).</para>
    /// <para>If not specified, the default value <see cref="DefaultKlinesQueryLimit"/> (500) is used.</para>
    /// </param>
    public IDeferredQuery<List<Candlestick>> PrepareGetContractPriceKlines(
        string pair,
        ContractType contractType,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfNotRunning();
        pair.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxKlinesQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(109);
        qs.AddParameter("pair", CommonUtility.NormalizeSymbol(pair));
        qs.AddParameter("contractType", FuturesUtility.Format(contractType));
        qs.AddParameter("interval", FuturesUtility.Format(interval));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));

        return new DeferredQuery<List<Candlestick>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetContractPriceKlinesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseCompleteCandlestickList,
            parseArgs: limit ?? DefaultKlinesQueryLimit,
            weights: GetCandlestickQueryWeight(limit ?? DefaultKlinesQueryLimit),
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets continuous kline/candlestick bars for the market price of a specific contract, asynchronously.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    public Task<List<Candlestick>> GetContractPriceKlinesAsync(
        string pair,
        ContractType contractType,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<Candlestick>> query =
            PrepareGetContractPriceKlines(pair, contractType, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get candlestick data on index price.
    /// <summary>
    /// Prepares a query for kline/candlestick bars for the index price of the specified trading pair.
    /// <para>The most recent data is returned within the specified period.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    /// <param name="pair">The trading pair symbol to get candlestick data for.</param>
    /// <param name="startTime">The time to fetch data from (inclusive).
    /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified,
    /// the interval between them must not exceed <see cref="MaxIndexPriceLookupInterval"/> (200 days).</para></param>
    /// <param name="endTime">The time to fetch data prior to (inclusive).
    /// <para>If not specified, the timestamp of 200 days after <paramref name="startTime"/> will be used (up to the current time)</para>
    /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified,
    /// the interval between them must not exceed <see cref="MaxIndexPriceLookupInterval"/> (200 days).</para></param>
    /// <param name="limit">
    /// The maximum number of candlesticks to fetch.
    /// <para>The maximum value is <see cref="MaxKlinesQueryLimit"/> (1500).</para>
    /// <para>If not specified, the default value <see cref="DefaultKlinesQueryLimit"/> (500) is used.</para>
    /// </param>
    public IDeferredQuery<List<Candlestick>> PrepareGetIndexPriceKlines(
        string pair,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfNotRunning();
        pair.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxKlinesQueryLimit);

        if (startTime != null && endTime != null)
        {
            if (endTime.Value < startTime.Value)
            {
                throw new ArgumentException($"The specified period [{startTime} - {endTime}] is invalid.");
            }
            if ((endTime.Value - startTime.Value).Ticks > MaxIndexPriceLookupInterval)
            {
                throw new ArgumentException($"The specified lookup period ({(endTime.Value - startTime.Value).TotalDays:0.0} days) is too large.");

            }
        }

        QueryBuilder qs = new QueryBuilder(109);
        qs.AddParameter("pair", CommonUtility.NormalizeSymbol(pair));
        qs.AddParameter("interval", FuturesUtility.Format(interval));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));

        return new DeferredQuery<List<Candlestick>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetIndexPriceKlinesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParsePartialCandlestickList,
            parseArgs: limit ?? DefaultKlinesQueryLimit,
            weights: GetCandlestickQueryWeight(limit ?? DefaultKlinesQueryLimit),
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets kline/candlestick bars for the index price of the specified trading pair, asynchronously.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    public Task<List<Candlestick>> GetIndexPriceKlinesAsync(
        string pair,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<Candlestick>> query =
            PrepareGetIndexPriceKlines(pair, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get candlestick data on mark price.
    /// <summary>
    /// Prepares a query for kline/candlestick bars for the mark price of the specified symbol.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    /// <param name="symbol">The futures contract symbol to get candlestick data for.</param>
    /// <param name="startTime">The time to fetch candlesticks from, based on their opening time (inclusive, optional).</param>
    /// <param name="endTime">The time to fetch candlesticks prior to, based on their opening time (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of candlesticks to fetch.
    /// <para>The maximum value is <see cref="MaxKlinesQueryLimit"/> (1500).</para>
    /// <para>If not specified, the default value <see cref="DefaultKlinesQueryLimit"/> (500) is used.</para>
    /// </param>
    public IDeferredQuery<List<Candlestick>> PrepareGetMarkPriceKlines(
        string symbol,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxKlinesQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(109);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        qs.AddParameter("interval", FuturesUtility.Format(interval));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));

        return new DeferredQuery<List<Candlestick>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetMarkPriceKlinesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParsePartialCandlestickList,
            parseArgs: limit ?? DefaultKlinesQueryLimit,
            weights: GetCandlestickQueryWeight(limit ?? DefaultKlinesQueryLimit),
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets kline/candlestick bars for the mark price of the specified trading pair, asynchronously.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    public Task<List<Candlestick>> GetMarkPriceKlinesAsync(
        string symbol, KlineInterval interval,
        DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<Candlestick>> query =
            PrepareGetMarkPriceKlines(symbol, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get candlesticks data on premium info.
    /// <summary>
    /// Prepares a query for kline/candlestick bars for the premium index of the specified symbol.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    /// <param name="symbol">The futures contract symbol to get candlestick data for.</param>
    /// <param name="startTime">The time to fetch candlesticks from, based on their opening time (inclusive, optional).</param>
    /// <param name="endTime">The time to fetch candlesticks prior to, based on their opening time (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of candlesticks to fetch.
    /// <para>The maximum value is <see cref="MaxKlinesQueryLimit"/> (1500).</para>
    /// <para>If not specified, the default value <see cref="DefaultKlinesQueryLimit"/> (500) is used.</para>
    /// </param>
    public IDeferredQuery<List<Candlestick>> PrepareGetPremiumIndexKlines(
        string symbol,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxKlinesQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(109);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        qs.AddParameter("interval", FuturesUtility.Format(interval));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));

        return new DeferredQuery<List<Candlestick>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetPremiumIndexKlinesEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParsePartialCandlestickList,
            parseArgs: limit ?? DefaultKlinesQueryLimit,
            weights: GetCandlestickQueryWeight(limit ?? DefaultKlinesQueryLimit),
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets kline/candlestick bars for the premium index of the specified trading pair, asynchronously.
    /// <para>Klines are uniquely identified by their open time.</para>
    /// <para>Note: the query weight depends on the value of <paramref name="limit"/>.
    /// The most weight-efficient value is 499 items per query.</para>
    /// </summary>
    public Task<List<Candlestick>> GetPremiumIndexKlinesAsync(
        string symbol, KlineInterval interval,
        DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<Candlestick>> query =
            PrepareGetPremiumIndexKlines(symbol, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get premium info.
    /// <summary>
    /// Prepares a query for the current premium info for all symbols.
    /// </summary>
    public IDeferredQuery<List<PremiumInfo>> PrepareGetPremiumInfo()
    {
        ThrowIfNotRunning();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 10)
        };

        return new DeferredQuery<List<PremiumInfo>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetPremiumInfoEndpoint, null, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParsePremiumInfoList,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets the current premium info for all symbols asynchronously.
    /// </summary>
    public Task<List<PremiumInfo>> GetPremiumInfoAsync(CancellationToken ct = default)
    {
        using (IDeferredQuery<List<PremiumInfo>> query = PrepareGetPremiumInfo())
        {
            return query.ExecuteAsync(ct);
        }
    }

    /// <summary>
    /// Prepares a query for the current premium info for all symbols with the specified underlying trading pair.
    /// </summary>
    public IDeferredQuery<List<PremiumInfo>> PrepareGetPremiumInfoOnPair(string pair)
    {
        ThrowIfNotRunning();
        pair.ThrowIfNullOrWhitespace();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 10)
        };

        QueryBuilder qs = new QueryBuilder(15);
        qs.AddParameter("pair", CommonUtility.NormalizeSymbol(pair));

        return new DeferredQuery<List<PremiumInfo>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetPremiumInfoEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParsePremiumInfoList,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets the current premium info for all symbols with the specified underlying trading pair, asynchronously.
    /// </summary>
    public Task<List<PremiumInfo>> GetPremiumInfoOnPairAsync(string pair, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<PremiumInfo>> query = PrepareGetPremiumInfoOnPair(pair))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<PremiumInfo> ParsePremiumInfoList(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<PremiumInfo> resultList = new List<PremiumInfo>(ExpectedSymbolsCount);
        ParseSchemaValidator? validator = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            resultList.Add(ParsePremiumInfo(ref reader, ref validator));
        }

        return resultList;
    }

    /// <summary>
    /// Prepares a query for the current premium info for the specified symbol.
    /// </summary>
    public IDeferredQuery<PremiumInfo> PrepareGetPremiumInfoOnSymbol(string symbol)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1)
        };

        QueryBuilder qs = new QueryBuilder(34);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));

        return new DeferredQuery<PremiumInfo>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetPremiumInfoEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseSinglePremiumInfo,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets the current premium info for the specified symbol asynchronously.
    /// </summary>
    public Task<PremiumInfo> GetPremiumInfoOnSymbolAsync(string symbol, CancellationToken ct = default)
    {
        using (IDeferredQuery<PremiumInfo> query = PrepareGetPremiumInfoOnSymbol(symbol))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private PremiumInfo ParseSinglePremiumInfo(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        ParseUtility.ReadObjectStart(ref reader);
        ParseSchemaValidator? validator = null;

        PremiumInfo result = ParsePremiumInfo(ref reader, ref validator);

        ParseUtility.ReadArrayEnd(ref reader);
        return result;
    }

    private PremiumInfo ParsePremiumInfo(ref Utf8JsonReader reader, ref ParseSchemaValidator? validator)
    {
        ParseUtility.EnsureObjectStartToken(ref reader);

        if (validator == null)
        {
            validator = new ParseSchemaValidator(9);
        }
        else
        {
            validator.Reset();
        }

        string? symbol = null, pair = null;
        decimal markPrice = 0.0m, indexPrice = 0.0m, settlePrice = 0.0m, lastFundRate = -1m, interestRate = -1m;
        DateTime nextFundTime = DateTime.MinValue, timestamp = DateTime.MinValue;

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
                    symbol = reader.GetString();
                    validator.RegisterProperty(0);
                    break;
                case "pair":
                    pair = reader.GetString();
                    validator.RegisterProperty(1);
                    break;
                case "markPrice":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out markPrice);
                    validator.RegisterProperty(2);
                    break;
                case "indexPrice":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out indexPrice);
                    validator.RegisterProperty(3);
                    break;
                case "estimatedSettlePrice":
                    ParseUtility.ParseDecimal(propName, reader.GetString(), out settlePrice);
                    validator.RegisterProperty(4);
                    break;
                case "lastFundingRate":
                    if (!string.IsNullOrEmpty(reader.GetString()))
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out lastFundRate);
                    validator.RegisterProperty(5);
                    break;
                case "nextFundingTime":
                    if (reader.GetInt64() != 0)
                        nextFundTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    validator.RegisterProperty(6);
                    break;
                case "interestRate":
                    if (!string.IsNullOrEmpty(reader.GetString()))
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out interestRate);
                    validator.RegisterProperty(7);
                    break;
                case "time":
                    timestamp = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    validator.RegisterProperty(8);
                    break;
                default:
                    throw ParseUtility.GenerateUnknownPropertyException(propName);
            }
        }

        // Check whether all the essential properties were provided.
        if (!validator.IsComplete())
        {
            const string objName = "premium info";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "pair"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "mark price"),
                3 => ParseUtility.GenerateMissingPropertyException(objName, "index price"),
                4 => ParseUtility.GenerateMissingPropertyException(objName, "estim settlement price"),
                5 => ParseUtility.GenerateMissingPropertyException(objName, "last funding rate"),
                6 => ParseUtility.GenerateMissingPropertyException(objName, "next funding time"),
                7 => ParseUtility.GenerateMissingPropertyException(objName, "interest rate"),
                8 => ParseUtility.GenerateMissingPropertyException(objName, "timestamp"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
            };
        }

        return new PremiumInfo(
            symbol!,
            pair!,
            markPrice,
            indexPrice,
            settlePrice,
            lastFundRate == -1m ? null : new decimal?(lastFundRate),
            interestRate == -1m ? null : new decimal?(interestRate),
            nextFundTime == DateTime.MinValue ? null : new DateTime?(nextFundTime),
            timestamp);
    }

    // Get funding rate history.
    /// <summary>
    /// Prepares a query for the funding rate history for the specified perpetual contract.
    /// </summary>
    /// <param name="symbol">The perpetual contract symbol to get history for.</param>
    /// <param name="startTime">The timestamp get funding rate from (inclusive, optional).</param>
    /// <param name="endTime">The timestamp get funding rate until (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of records to fetch.
    /// <para>The maximum value is <see cref="MaxFundingHistoryQueryLimit"/> (1000).</para>
    /// <para>If not specified, the default value <see cref="DefaultFundingHistoryQueryLimit"/> (100) is used.</para>
    /// </param>
    public IDeferredQuery<List<FundingRate>> PrepareGetFundingRateHistory(
        string symbol,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfDisposed();
        symbol.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxFundingHistoryQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
        };

        QueryBuilder qs = new QueryBuilder(74);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);

        return new DeferredQuery<List<FundingRate>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetFundingRateHistoryEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseFundingRateList,
            parseArgs: limit,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets the funding rate history for the specified perpetual contract asynchronously.
    /// </summary>
    public Task<List<FundingRate>> GetFundingRateHistoryAsync(
        string? symbol = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<FundingRate>> query = PrepareGetFundingRateHistory(symbol, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<FundingRate> ParseFundingRateList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<FundingRate> resultList = new List<FundingRate>(
            parseArgs is int expectedCount ? expectedCount : DefaultFundingHistoryQueryLimit);

        ParseSchemaValidator validator = new ParseSchemaValidator(4);
        string? symbol = null;
        decimal rate = 0.0m;
        decimal markPrice = 0.0m;
        DateTime time = DateTime.MinValue;

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
                    case "symbol":
                        symbol = reader.GetString();
                        validator.RegisterProperty(0);
                        break;
                    case "fundingRate":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out rate);
                        validator.RegisterProperty(1);
                        break;
                    case "fundingTime":
                        time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(2);
                        break;
                    case "markPrice":
                        if (string.IsNullOrEmpty(reader.GetString()))
                        {
                            markPrice = 0.0m;
                        }
                        else
                        {
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out markPrice);
                        }
                        validator.RegisterProperty(3);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "funding rate record";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                    1 => ParseUtility.GenerateMissingPropertyException(objName, "rate"),
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "time"),
                    3 => ParseUtility.GenerateMissingPropertyException(objName, "mark price"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
                };
            }

            // Add the trade to the results list.
            resultList.Add(new FundingRate(symbol!, time, rate, markPrice != 0.0m ? markPrice : null));
            validator.Reset();
        }

        // Return the result.
        return resultList;
    }

    // Get funding rate info.
    /// <summary>
    /// Prepares a query for funding rate configurations of all perpetual symbols.
    /// </summary>
    public IDeferredQuery<List<FundingRateConfig>> PrepareGetFundingRateInfo()
    {
        ThrowIfDisposed();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
        };

        return new DeferredQuery<List<FundingRateConfig>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetFundingRateInfoEndpoint, null, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseFundingRateConfigList,
            parseArgs: null,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Gets funding rate configurations of all perpetual symbols asynchronously.
    /// </summary>
    public Task<List<FundingRateConfig>> GetGetFundingRateInfoAsync(CancellationToken ct = default)
    {
        using (IDeferredQuery<List<FundingRateConfig>> query = PrepareGetFundingRateInfo())
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<FundingRateConfig> ParseFundingRateConfigList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<FundingRateConfig> resultList = new(ExpectedSymbolsCount);

        ParseSchemaValidator validator = new ParseSchemaValidator(4);
        string? symbol = null;
        decimal cap = 0.0m, floor = 0.0m;
        int interval = -1;

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
                    case "symbol":
                        symbol = reader.GetString();
                        validator.RegisterProperty(0);
                        break;
                    case "adjustedFundingRateCap":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out cap);
                        validator.RegisterProperty(1);
                        break;
                    case "adjustedFundingRateFloor":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out floor);
                        validator.RegisterProperty(2);
                        break;
                    case "fundingIntervalHours":
                        interval = reader.GetInt32();
                        validator.RegisterProperty(3);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "funding rate config";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                    1 => ParseUtility.GenerateMissingPropertyException(objName, "funding rate cap"),
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "funding rate floor"),
                    3 => ParseUtility.GenerateMissingPropertyException(objName, "funding interval"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
                };
            }

            // Add the trade to the results list.
            resultList.Add(new FundingRateConfig(symbol!, cap, floor, interval));
            validator.Reset();
        }

        // Return the result.
        return resultList;
    }

    // Get current open interest.
    /// <summary>
    /// Prepares a query for the present open interest for the specified symbol.
    /// </summary>
    /// <param name="symbol">The futures contract symbol to get open interest for.</param>
    public IDeferredQuery<OpenInterest> PrepareGetOpenInterest(string symbol)
    {
        ThrowIfNotRunning();
        symbol.ThrowIfNullOrWhitespace();

        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1)
        };

        QueryBuilder qs = new QueryBuilder(23);
        qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));

        return new DeferredQuery<OpenInterest>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetCurrentOpenInterestEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseSingleOpenInterest,
            weights: weights,
            headersToLimitsMap: _dummyHeadersLimitsMap);
    }

    private OpenInterest ParseSingleOpenInterest(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);

        ParseSchemaValidator validator = new ParseSchemaValidator(5);
        double quantity = double.NaN;
        DateTime time = DateTime.MinValue;

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
                    validator.RegisterProperty(0);
                    break;
                case "pair":
                    validator.RegisterProperty(1);
                    break;
                case "contractType":
                    validator.RegisterProperty(2);
                    break;
                case "openInterest":
                    ParseUtility.ParseDouble(propName, reader.GetString(), out quantity);
                    validator.RegisterProperty(3);
                    break;
                case "time":
                    time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                    validator.RegisterProperty(4);
                    break;
                default:
                    throw ParseUtility.GenerateUnknownPropertyException(propName);
            }
        }

        // Check whether all the essential properties were provided.
        if (!validator.IsComplete())
        {
            const string objName = "open interest record";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "trading pair"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "contract type"),
                3 => ParseUtility.GenerateMissingPropertyException(objName, "total quantity"),
                4 => ParseUtility.GenerateMissingPropertyException(objName, "timestamp"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
            };
        }

        // Return the result.
        return new OpenInterest(time, quantity, double.NaN);
    }

    // Get open interest history.
    /// <summary>
    /// Prepares a query for the open interest history for contract type(s) with the specified underlying pair.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    /// <param name="pair">The underlying trading pair to get history for.</param>
    /// <param name="contractType">
    /// The futures contract type to get history for.
    /// <para>Use the value <c>Null</c> to request data for all contract types.</para>
    /// <para>Only the following types are supported: <see cref="ContractType.Perpetual"/>,
    /// <see cref="ContractType.CurrentQuarter"/>, <see cref="ContractType.NextQuarter"/>.</para>
    /// </param>
    /// <param name="startTime">The timestamp get data from (inclusive, optional).</param>
    /// <param name="endTime">The timestamp get data until (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of records to fetch.
    /// <para>The maximum value is <see cref="MaxMarketStatsQueryLimit"/> (500).</para>
    /// <para>If not specified, the default value <see cref="DefaultMarketStatsQueryLimit"/> (30) is used.</para>
    /// </param>
    public IDeferredQuery<List<OpenInterest>> PrepareGetOpenInterestHistory(
        string pair,
        ContractType? contractType,
        StatsInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfDisposed();
        pair.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxMarketStatsQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(110);
        qs.AddParameter("pair", CommonUtility.NormalizeSymbol(pair));
        qs.AddParameter("contractType", contractType == null ? "ALL" : FuturesUtility.Format(contractType.Value));
        qs.AddParameter("period", FuturesUtility.Format(interval));
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);

        return new DeferredQuery<List<OpenInterest>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetOpenInterestHistoryEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseOpenInterestList,
            parseArgs: limit,
            headersToLimitsMap: _dummyHeadersLimitsMap);
    }

    /// <summary>
    /// Gets the open interest history for contract type(s) with the specified underlying pair, asynchronously.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    public Task<List<OpenInterest>> GetOpenInterestHistoryAsync(
        string pair, ContractType? contractType, StatsInterval interval,
        DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<OpenInterest>> query =
            PrepareGetOpenInterestHistory(pair, contractType, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<OpenInterest> ParseOpenInterestList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<OpenInterest> resultList = new List<OpenInterest>(
            parseArgs is int expectedCount ? expectedCount : DefaultMarketStatsQueryLimit);

        ParseSchemaValidator validator = new ParseSchemaValidator(5);
        double quantity = double.NaN, value = double.NaN;
        DateTime time = DateTime.MinValue;

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
                    case "pair":
                        validator.RegisterProperty(0);
                        break;
                    case "contractType":
                        validator.RegisterProperty(1);
                        break;
                    case "sumOpenInterest":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out quantity);
                        validator.RegisterProperty(2);
                        break;
                    case "sumOpenInterestValue":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out value);
                        validator.RegisterProperty(3);
                        break;
                    case "timestamp":
                        time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(4);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "open interest record";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => ParseUtility.GenerateMissingPropertyException(objName, "pair"),
                    1 => ParseUtility.GenerateMissingPropertyException(objName, "contract type"),
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "total quantity"),
                    3 => ParseUtility.GenerateMissingPropertyException(objName, "total value"),
                    4 => ParseUtility.GenerateMissingPropertyException(objName, "timestamp"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
                };
            }

            // Add the trade to the results list.
            resultList.Add(new OpenInterest(time, quantity, value));
            validator.Reset();
        }

        // Return the result.
        return resultList;
    }

    // Get top trader accounts long/short ratio.
    /// <summary>
    /// Prepares a query for the history of the long/short ratio of top trader accounts.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    /// <param name="pair">The underlying trading pair symbol to get history for.</param>
    /// <param name="startTime">The timestamp get data from (inclusive, optional).</param>
    /// <param name="endTime">The timestamp get data until (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of records to fetch.
    /// <para>The maximum value is <see cref="MaxMarketStatsQueryLimit"/> (500).</para>
    /// <para>If not specified, the default value <see cref="DefaultMarketStatsQueryLimit"/> (30) is used.</para>
    /// </param>
    public IDeferredQuery<List<LongShortRatio>> PrepareGetTopAccountLongShortRatio(
        string pair,
        StatsInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfDisposed();
        pair.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxMarketStatsQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(83);
        qs.AddParameter("pair", CommonUtility.NormalizeSymbol(pair));
        qs.AddParameter("period", FuturesUtility.Format(interval));
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);

        return new DeferredQuery<List<LongShortRatio>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetTopAccountLSRatioEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseLongShortRatioList,
            parseArgs: limit,
            headersToLimitsMap: _dummyHeadersLimitsMap);
    }

    /// <summary>
    /// Gets the history of the long/short ratio of top trader accounts, asynchronously.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    public Task<List<LongShortRatio>> GetTopAccountLongShortRatioAsync(
        string pair, StatsInterval interval,
        DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken ct = default)
    {
        using (IDeferredQuery<List<LongShortRatio>> query =
            PrepareGetTopAccountLongShortRatio(pair, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get top trader positions long/short ratio.
    /// <summary>
    /// Prepares a query for the history of the long/short ratio of top trader positions.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    /// <param name="pair">The underlying trading pair symbol to get history for.</param>
    /// <param name="startTime">The timestamp get data from (inclusive, optional).</param>
    /// <param name="endTime">The timestamp get data until (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of records to fetch.
    /// <para>The maximum value is <see cref="MaxMarketStatsQueryLimit"/> (500).</para>
    /// <para>If not specified, the default value <see cref="DefaultMarketStatsQueryLimit"/> (30) is used.</para>
    /// </param>
    public IDeferredQuery<List<LongShortRatio>> PrepareGetTopPositionLongShortRatio(
        string pair,
        StatsInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfDisposed();
        pair.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxMarketStatsQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(83);
        qs.AddParameter("pair", CommonUtility.NormalizeSymbol(pair));
        qs.AddParameter("period", FuturesUtility.Format(interval));
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);

        return new DeferredQuery<List<LongShortRatio>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetTopPositionLSRatioEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseLongShortRatioList,
            parseArgs: limit,
            headersToLimitsMap: _dummyHeadersLimitsMap);
    }

    /// <summary>
    /// Gets the history of the long/short ratio of top trader positions, asynchronously.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    public Task<List<LongShortRatio>> GetTopPositionLongShortRatioAsync(
        string pair,
        StatsInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<LongShortRatio>> query =
            PrepareGetTopPositionLongShortRatio(pair, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get all accounts long/short ratio.
    /// <summary>
    /// Prepares a query for the global history of the long/short ratio of accounts.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    /// <param name="pair">The underlying trading pair symbol to get history for.</param>
    /// <param name="startTime">The timestamp get data from (inclusive, optional).</param>
    /// <param name="endTime">The timestamp get data until (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of records to fetch.
    /// <para>The maximum value is <see cref="MaxMarketStatsQueryLimit"/> (500).</para>
    /// <para>If not specified, the default value <see cref="DefaultMarketStatsQueryLimit"/> (30) is used.</para>
    /// </param>
    public IDeferredQuery<List<LongShortRatio>> PrepareGetAllAccountLongShortRatio(
        string pair,
        StatsInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfDisposed();
        pair.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxMarketStatsQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(83);
        qs.AddParameter("pair", CommonUtility.NormalizeSymbol(pair));
        qs.AddParameter("period", FuturesUtility.Format(interval));
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);

        return new DeferredQuery<List<LongShortRatio>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetAllAccountLSRatioEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseLongShortRatioList,
            parseArgs: limit,
            headersToLimitsMap: _dummyHeadersLimitsMap);
    }

    /// <summary>
    /// Gets the global history of the long/short ratio of accounts asynchronously.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    public Task<List<LongShortRatio>> GetAllAccountLongShortRatioAsync(
        string pair,
        StatsInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<LongShortRatio>> query =
            PrepareGetAllAccountLongShortRatio(pair, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    // Get taker trades volume.
    /// <summary>
    /// Prepares a query for the history of buy/sell volume of "taker" trades of futures contract(s).
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    /// <param name="pair">The underlying trading pair of contract(s) to get history for.</param>
    /// <param name="contractType">
    /// The futures contract type to get history for.
    /// <para>Use the value <c>Null</c> to request data for all contract types.</para>
    /// <para>Only the following types are supported: <see cref="ContractType.Perpetual"/>,
    /// <see cref="ContractType.CurrentQuarter"/>, <see cref="ContractType.NextQuarter"/>.</para>
    /// </param>
    /// <param name="startTime">The timestamp get data from (inclusive, optional).</param>
    /// <param name="endTime">The timestamp get data until (inclusive, optional).</param>
    /// <param name="limit">
    /// The maximum number of records to fetch.
    /// <para>The maximum value is <see cref="MaxMarketStatsQueryLimit"/> (500).</para>
    /// <para>If not specified, the default value <see cref="DefaultMarketStatsQueryLimit"/> (30) is used.</para>
    /// </param>
    public IDeferredQuery<List<TakerVolume>> PrepareGetTakerVolume(
        string pair,
        ContractType? contractType,
        StatsInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        ThrowIfDisposed();
        pair.ThrowIfNullOrWhitespace();
        limit.ThrowIfInvalidLimit(MaxMarketStatsQueryLimit);
        ExceptionUtility.ThrowIfInvalidPeriod(startTime, endTime);

        QueryBuilder qs = new QueryBuilder(110);
        qs.AddParameter("pair", CommonUtility.NormalizeSymbol(pair));
        qs.AddParameter("contractType", contractType == null ? "ALL" : FuturesUtility.Format(contractType.Value));
        qs.AddParameter("period", FuturesUtility.Format(interval));
        if (startTime != null)
            qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
        if (endTime != null)
            qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
        if (limit != null)
            qs.AddParameter("limit", limit.Value);

        return new DeferredQuery<List<TakerVolume>>(
            query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, GetTakerBuySellVolumeEndpoint, qs, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseTakerVolumeList,
            parseArgs: limit,
            headersToLimitsMap: _dummyHeadersLimitsMap);
    }

    /// <summary>
    /// Gets the history of buy/sell volume of "taker" trades of futures contract(s), asynchronously.
    /// <para>Note: only the data of the latest 30 days is available.</para>
    /// </summary>
    public Task<List<TakerVolume>> GetTakerVolumeAsync(
        string pair,
        ContractType? contractType,
        StatsInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        using (IDeferredQuery<List<TakerVolume>> query =
            PrepareGetTakerVolume(pair, contractType, interval, startTime, endTime, limit))
        {
            return query.ExecuteAsync(ct);
        }
    }

    private List<TakerVolume> ParseTakerVolumeList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<TakerVolume> results = new List<TakerVolume>(
            parseArgs is int expectedCount ? expectedCount : DefaultMarketStatsQueryLimit);

        ParseSchemaValidator validator = new ParseSchemaValidator(7);
        DateTime timestamp = DateTime.MinValue;
        double buyVol = double.NaN, sellVol = double.NaN, buyVal = double.NaN, sellVal = double.NaN;

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
                    case "pair":
                        validator.RegisterProperty(0);
                        break;
                    case "contractType":
                        validator.RegisterProperty(1);
                        break;
                    case "takerBuyVol":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out buyVol);
                        validator.RegisterProperty(2);
                        break;
                    case "takerSellVol":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out sellVol);
                        validator.RegisterProperty(3);
                        break;
                    case "takerBuyVolValue":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out buyVal);
                        validator.RegisterProperty(4);
                        break;
                    case "takerSellVolValue":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out sellVal);
                        validator.RegisterProperty(5);
                        break;
                    case "timestamp":
                        timestamp = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(6);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "taker volume record";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => ParseUtility.GenerateMissingPropertyException(objName, "pair"),
                    1 => ParseUtility.GenerateMissingPropertyException(objName, "contract type"),
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "buy volume"),
                    3 => ParseUtility.GenerateMissingPropertyException(objName, "sell volume"),
                    4 => ParseUtility.GenerateMissingPropertyException(objName, "total buy value"),
                    5 => ParseUtility.GenerateMissingPropertyException(objName, "total sell value"),
                    6 => ParseUtility.GenerateMissingPropertyException(objName, "timestamp"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
                };
            }

            // Add the record to the results list.
            results.Add(new TakerVolume(timestamp, double.NaN, buyVol, sellVol, buyVal, sellVal));
            validator.Reset();
        }

        return results;
    }

    // Queries common logic.
    private QueryWeight[] GetCandlestickQueryWeight(int itemsLimit)
    {
        QueryWeight[] weights = new QueryWeight[1];

        if (itemsLimit > 1000)
            weights[0] = new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 10);
        else if (itemsLimit > 499)
            weights[0] = new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 5);
        else if (itemsLimit > 99)
            weights[0] = new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 2);
        else
            weights[0] = new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1);

        return weights;
    }

    /// <summary>
    /// Parses a list of complete candlesticks from the given JSON data.
    /// </summary>
    [SuppressMessage("Style", "IDE0018:Inline variable declaration", Justification = "Explicit declaration is preferred.")]
    private List<Candlestick> ParseCompleteCandlestickList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<Candlestick> results = new List<Candlestick>(
            parseArgs is int expectedCount ? expectedCount : DefaultKlinesQueryLimit);
        DateTime openTime, closeTime;
        decimal o, h, l, c, bv, qv, tbv, tqv;
        uint numOfTrades;

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

    /// <summary>
    /// Parses a list of partial candlesticks from the given JSON data.
    /// </summary>
    [SuppressMessage("Style", "IDE0018:Inline variable declaration", Justification = "Explicit declaration is preferred.")]
    private List<Candlestick> ParsePartialCandlestickList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<Candlestick> results = new List<Candlestick>(
            parseArgs is int expectedCount ? expectedCount : DefaultKlinesQueryLimit);
        DateTime openTime, closeTime;
        decimal o, h, l, c;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ParseUtility.EnsureArrayStartToken(ref reader);

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"A candlestick open time value was expected but \"{reader.TokenType}\" encountered.");
            openTime = CommonUtility.ConvertToDateTime(reader.GetInt64());

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick open measure value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("open", reader.GetString(), out o);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick high measure value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("high", reader.GetString(), out h);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick low measure value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("low", reader.GetString(), out l);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A candlestick close measure value was expected but \"{reader.TokenType}\" encountered.");
            ParseUtility.ParseDecimal("close", reader.GetString(), out c);

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"The 6-th value of the string type was expected but \"{reader.TokenType}\" encountered.");

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"A candlestick close time value was expected but \"{reader.TokenType}\" encountered.");
            closeTime = CommonUtility.ConvertToDateTime(reader.GetInt64());

            if (!reader.Read())
                throw new JsonException($"The 8-th value was expected but \"{reader.TokenType}\" encountered.");

            if (!reader.Read())
                throw new JsonException($"The 9-th value was expected but \"{reader.TokenType}\" encountered.");

            if (!reader.Read())
                throw new JsonException($"The 10-th string value was expected but \"{reader.TokenType}\" encountered.");

            if (!reader.Read())
                throw new JsonException($"The 11-th string value was expected but \"{reader.TokenType}\" encountered.");

            if (!reader.Read())
                throw new JsonException($"The last property value was expected but \"{reader.TokenType}\" encountered.");

            ParseUtility.ReadArrayEnd(ref reader);
            results.Add(new Candlestick(openTime, closeTime, o, h, l, c));
        }

        // Add the candlestick to the results list.
        return results;
    }

    private List<LongShortRatio> ParseLongShortRatioList(byte[] data, object? parseArgs = null)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

        ParseUtility.ReadArrayStart(ref reader);
        List<LongShortRatio> results = new List<LongShortRatio>(
            parseArgs is int expectedCount ? expectedCount : DefaultMarketStatsQueryLimit);

        ParseSchemaValidator validator = new ParseSchemaValidator(5);
        DateTime timestamp = DateTime.MinValue;
        double ratio = double.NaN, longs = double.NaN, shorts = double.NaN;

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
                    case "pair":
                        validator.RegisterProperty(0);
                        break;
                    case "longShortRatio":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out ratio);
                        validator.RegisterProperty(1);
                        break;
                    case "longAccount":
                    case "longPosition":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out longs);
                        validator.RegisterProperty(2);
                        break;
                    case "shortAccount":
                    case "shortPosition":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out shorts);
                        validator.RegisterProperty(3);
                        break;
                    case "timestamp":
                        timestamp = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(4);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "long/short ratio record";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => ParseUtility.GenerateMissingPropertyException(objName, "symbol"),
                    1 => ParseUtility.GenerateMissingPropertyException(objName, "long/short ratio"),
                    2 => ParseUtility.GenerateMissingPropertyException(objName, "longs value"),
                    3 => ParseUtility.GenerateMissingPropertyException(objName, "shorts value"),
                    4 => ParseUtility.GenerateMissingPropertyException(objName, "timestamp"),
                    _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown {missingPropNum}"),
                };
            }

            // Add the record to the results list.
            results.Add(new LongShortRatio(timestamp, ratio, longs, shorts));
            validator.Reset();
        }

        return results;
    }

    #endregion
}
