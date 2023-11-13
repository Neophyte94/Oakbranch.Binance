﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.RateLimits;
using Oakbranch.Binance.Utility;

namespace Oakbranch.Binance.Futures.USDM;

/// <summary>
/// Provides common functionality for API client classes using "/fapi/v1" endpoints.
/// </summary>
public abstract class FuturesUMClientBase : SharedLimitsApiClientBase
{
    #region Constants

    private const string LimitsDiscrimativeEndpoint = "/fapi/v1";
    private const string GetConnectivityEndpoint = "/fapi/v1/ping";
    protected const string GetExchangeInfoEndpoint = "/fapi/v1/exchangeInfo";

    #endregion

    #region Static members

    private static readonly ReadOnlyCollection<BaseEndpoint> s_RESTBaseEndpoints;
    /// <summary>
    /// Gets a list of all available base endpoints for main API requests.
    /// <para>The main API include market data, spot, margin, wallet and savings endpoints.</para>
    /// </summary>
    public static IReadOnlyList<BaseEndpoint> RESTBaseEndpoints => s_RESTBaseEndpoints;

    #endregion

    #region Instance members

    private BaseEndpoint _RESTEndpoint;
    /// <summary>
    /// Gets ot sets the base endpoint used for REST API requests.
    /// </summary>
    public BaseEndpoint RESTEndpoint
    {
        get
        {
            return _RESTEndpoint;
        }
        set
        {
            if (_RESTEndpoint == value) return;
            if (!s_RESTBaseEndpoints.Contains(value))
            {
                throw new ArgumentException(
                    $"The specified base endpoint \"{value}\" is not one of the supported main base endpoints. " +
                    $"Please use one of the endpoints listed in {nameof(ApiConnector)}.{nameof(RESTBaseEndpoints)}.");
            }
            _RESTEndpoint = value;
        }
    }

    #endregion

    #region Static constructor

    static FuturesUMClientBase()
    {
        s_RESTBaseEndpoints = new ReadOnlyCollection<BaseEndpoint>(new BaseEndpoint[]
        {
            new BaseEndpoint(NetworkType.Live, "https://fapi.binance.com", "FAPI live network"),
            new BaseEndpoint(NetworkType.Test, "https://testnet.binancefuture.com", "FAPI test network"),
        });
    }

    #endregion

    #region Instance constructors

    public FuturesUMClientBase(IApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger? logger = null)
        : base(connector, limitsRegistry, LimitsDiscrimativeEndpoint, logger)
    {
        _RESTEndpoint = s_RESTBaseEndpoints.First((bep) => bep.Type == NetworkType.Live);
    }

    #endregion

    #region Static methods

    private static List<RateLimiter> ParseRateLimiters(byte[] data)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
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
                case "rateLimits":
                    ParseUtility.EnsureArrayStartToken(ref reader);
                    List<RateLimiter> limiters = new List<RateLimiter>(6);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        limiters.Add(ParseRateLimiter(ref reader));
                    }
                    return limiters;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("The response data contains no rate limits info.");
    }

    #endregion

    #region Instance methods

    // Initialization.
    protected override async Task InitializeProtectedAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Send the initialization query.
        QueryParams initQueryParams = new QueryParams(HttpMethod.GET, _RESTEndpoint.Url, GetExchangeInfoEndpoint, null);
        Response rsp = await Connector.SendAsync(initQueryParams, ct).ConfigureAwait(false);

        // Parse the response.
        EnsureSuccessfulInitResponse(rsp);
        List<RateLimiter> limits = ParseRateLimiters(rsp.Content);
        Dictionary<string, int> headersLimitsMap = new Dictionary<string, int>(limits.Count);

        // Register or update rate limits in the registry.
        RegisterOrUpdateRateLimits(limits);
    }

    protected override string? GetRateLimitHeaderName(RateLimiter limit)
    {
        return limit.Type switch
        {
            RateLimitType.UID => $"X-MBX-ORDER-COUNT-{limit.IntervalNumber}{Format(limit.Interval)}",
            RateLimitType.IP => $"X-MBX-USED-WEIGHT-{limit.IntervalNumber}{Format(limit.Interval)}",
            RateLimitType.RawRequests => null,
            _ => throw new NotImplementedException($"An unknown rate limit type \"{limit.Type}\" was specified."),
        };
    }

    // Test connectivity.
    /// <summary>
    /// Creates a deferred query to test connectivity to the Binance Rest API.
    /// </summary>
    public IDeferredQuery<bool> PrepareTestConnectivity()
    {
        QueryWeight[] weights = new QueryWeight[]
        {
            new QueryWeight(GetWeightDimensionId(RateLimitType.RawRequests), 1),
            new QueryWeight(GetWeightDimensionId(RateLimitType.IP), 1),
        };
        return new DeferredQuery<bool>(
            query: new QueryParams(HttpMethod.GET, _RESTEndpoint.Url, GetConnectivityEndpoint, null, false),
            executeHandler: ExecuteQueryAsync,
            parseHandler: ParseConnectivityTestResponse,
            weights: weights,
            headersToLimitsMap: HeadersToLimitsMap);
    }

    /// <summary>
    /// Tests connectivity to the Binance Rest API.
    /// </summary>
    public Task<bool> TestConnectivityAsync(CancellationToken ct)
    {
        using (IDeferredQuery<bool> query = PrepareTestConnectivity())
        {
            return query.ExecuteAsync(ct);
        }
    }

    private bool ParseConnectivityTestResponse(byte[] data, object? _)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
        ParseUtility.ReadObjectStart(ref reader);
        ParseUtility.ReadObjectEnd(ref reader);

        return true;
    }

    #endregion
}
