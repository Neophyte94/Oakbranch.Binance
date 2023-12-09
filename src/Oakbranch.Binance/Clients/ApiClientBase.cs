using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Abstractions;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Core.RateLimits;
using Oakbranch.Binance.Exceptions;
using Oakbranch.Binance.Models;
using Oakbranch.Binance.Utility;

namespace Oakbranch.Binance.Clients;

/// <summary>
/// Provides base functionality for high-level client classes built upon specific Binance API areas.
/// <para>Implements the <see cref="IDisposable"/> interface.</para>
/// </summary>
public abstract class ApiClientBase : IDisposable
{
    #region Nested types

    private enum ClientState
    {
        Created,
        Running,
        Disposed
    }

    #endregion

    #region Instance props & fields

    private IApiConnector _connector;
    /// <summary>
    /// Gets the API connector used by the client for accessing low-level functions. 
    /// </summary>
    public IApiConnector Connector
    {
        get
        {
            ThrowIfDisposed();
            return _connector;
        }
    }

    private IRateLimitsRegistry _limitsRegistry;
    /// <summary>
    /// Gets the rate limits registry used by the client for checking and updating API rate limits.
    /// </summary>
    public IRateLimitsRegistry LimitsRegistry => _limitsRegistry;

    private ILogger? _logger;
    private Task? _initializationTask;

    private ClientState _state;
    /// <summary>
    /// Gets a value indicating whether the client has been initialized and not disposed yet.
    /// </summary>
    public bool IsInitialized => _state == ClientState.Running;
    /// <summary>
    /// Gets a value indicating whether the client has been disposed.
    /// </summary>
    public bool IsDisposed => _state == ClientState.Disposed;

    #endregion

    #region Instance constructors

    /// <summary>
    /// Creates an instance of the <see cref="ApiClientBase"/> class with the provided components.
    /// </summary>
    /// <param name="connector">The API connector to use for accessing low-level web functions.</param>
    /// <param name="limitsRegistry">The rate limits registry to use for checking and updating API rate limits.</param>
    /// <param name="logger">The logger to use for posting log messages.</param>
    /// <exception cref="ArgumentNullException"/>
    internal ApiClientBase(
        IApiConnector connector,
        IRateLimitsRegistry limitsRegistry,
        ILogger? logger = null)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        _limitsRegistry = limitsRegistry ?? throw new ArgumentNullException(nameof(limitsRegistry));
        _logger = logger;
    }

    #endregion

    #region Static methods

    // Validation.
    /// <summary>
    /// Checks whether the specified response to an initialization query is successful,
    /// and throws <see cref="Exception"/> if it is not.
    /// <para>For a failed response, tries to parse its content and include the error message in the exception.</para>
    /// </summary>
    /// <param name="initQueryResponse">The response to the initialization query.</param>
    /// <exception cref="Exception"/>
    protected static void EnsureSuccessfulInitResponse(Response initQueryResponse)
    {
        if (!initQueryResponse.IsSuccessful)
        {
            if (initQueryResponse.Content == null)
            {
                throw new Exception("The initialization query failed for an unknown reason.");
            }

            ApiErrorInfo? errorInfo = null;
            try { errorInfo = ParseUtility.ParseErrorInfo(initQueryResponse.Content); }
            catch { }

            if (errorInfo != null)
            {
                throw new Exception($"The initialization query failed: {errorInfo.Value.Message}.");
            }
            else
            {
                throw new Exception(CommonUtility.DecodeByteContent(initQueryResponse.Content));
            }
        }
    }

    // Formatting and parsing.
    /// <summary>
    /// Converts the given time unit into its laconic string representation.
    /// </summary>
    /// <param name="unit">The time interval unit to format.</param>
    /// <returns>The string representation of the specified time unit.</returns>
    /// <exception cref="ArgumentException"/>
    protected static string Format(Interval unit)
    {
        return unit switch
        {
            Interval.Minute => "M",
            Interval.Second => "S",
            Interval.Day => "D",
            _ => throw new NotImplementedException(unit.ToString())
        };
    }

    protected static string Format(OrderSide value)
    {
        return value == OrderSide.Buy ? "BUY" : "SELL";
    }

    /// <summary>
    /// Parses a single rate limiter from the current position in the given JSON reader.
    /// </summary>
    /// <param name="reader">The JSON reader to parse from.</param>
    /// <returns>An instance of the <see cref="RateLimiter"/> struct parsed from JSON.</returns>
    /// <exception cref="JsonException"/>
    protected static RateLimiter ParseRateLimiter(ref Utf8JsonReader reader)
    {
        ParseUtility.EnsureObjectStartToken(ref reader);

        ParseSchemaValidator validator = new ParseSchemaValidator(4);
        RateLimitType limitType = default;
        Interval interval = default;
        ushort intervalNum = default;
        uint limit = default;
        uint? usage = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
            {
                throw ParseUtility.GenerateNoPropertyValueException(propName);
            }

            switch (propName)
            {
                case "rateLimitType":
                    limitType = ParseRateLimitType(ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(0);
                    break;

                case "interval":
                    interval = ParseInterval(ParseUtility.GetNonEmptyString(ref reader, propName));
                    validator.RegisterProperty(1);
                    break;

                case "intervalNum":
                    if (reader.TokenType != JsonTokenType.Number)
                        throw ParseUtility.GenerateInvalidValueTypeException(propName, JsonTokenType.Number, reader.TokenType);
                    intervalNum = reader.GetUInt16();
                    validator.RegisterProperty(2);
                    break;

                case "limit":
                    if (reader.TokenType != JsonTokenType.Number)
                        throw ParseUtility.GenerateInvalidValueTypeException(propName, JsonTokenType.Number, reader.TokenType);
                    limit = reader.GetUInt32();
                    validator.RegisterProperty(3);
                    break;

                case "count":
                    if (reader.TokenType != JsonTokenType.Number)
                        throw ParseUtility.GenerateInvalidValueTypeException(propName, JsonTokenType.Number, reader.TokenType);
                    usage = reader.GetUInt32();
                    break;
            }
        }

        // Ensure that all the essental properties were provided.
        if (!validator.IsComplete())
        {
            const string objName = "rate limiter";
            int missingPropNum = validator.GetMissingPropertyNumber();
            throw missingPropNum switch
            {
                0 => ParseUtility.GenerateMissingPropertyException(objName, "limit type"),
                1 => ParseUtility.GenerateMissingPropertyException(objName, "interval unit"),
                2 => ParseUtility.GenerateMissingPropertyException(objName, "interval number"),
                3 => ParseUtility.GenerateMissingPropertyException(objName, "limit"),
                _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
            };
        }

        // Create a rate limiter instance and return it.
        return new RateLimiter(limitType, interval, intervalNum, limit, usage);
    }

    private static RateLimitType ParseRateLimitType(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentNullException(nameof(s));

        return s switch
        {
            "RAW_REQUESTS" => RateLimitType.RawRequests,
            "REQUEST_WEIGHT" => RateLimitType.IP,
            "ORDERS" => RateLimitType.UID,
            _ => throw new JsonException($"An unknown rate limit type was encountered: \"{s}\"."),
        };
    }

    private static Interval ParseInterval(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentNullException(nameof(s));

        return s switch
        {
            "MINUTE" => Interval.Minute,
            "SECOND" => Interval.Second,
            "HOUR" => Interval.Hour,
            "DAY" => Interval.Day,
            _ => throw new JsonException($"An unknown rate limit interval was encountered: \"{s}\"."),
        };
    }

    // Factory utility.
    /// <summary>
    /// Generates a deterministic identifier for a query weight dimension with the given parameters.
    /// </summary>
    /// <param name="discriminativeEndpoint">The endpoint that the query weight dimension is applied to.</param>
    /// <param name="limitType">Th limit type associated with the query weight dimension.</param>
    /// <returns></returns>
    protected static int GenerateWeightDimensionId(string discriminativeEndpoint, RateLimitType limitType)
    {
        return unchecked(
            -673990 * CommonUtility.GetDeterministicHashCode(discriminativeEndpoint)
            + 307 * (int)limitType);
    }

    /// <summary>
    /// Generates a deterministic identifier for a API rate limit with the given parameters.
    /// </summary>
    /// <param name="dimensionId">The query weight dimension associated with the API rate limit.</param>
    /// <param name="resetInterval">The time interval that the API rate limit is applied to.</param>
    /// <returns></returns>
    protected static int GenerateRateLimitId(int dimensionId, TimeSpan resetInterval)
    {
        return unchecked(3105 * dimensionId - 9494 * (int)resetInterval.Ticks);
    }

    /// <summary>
    /// Converts the given response format error into a corresponding query exception.
    /// </summary>
    /// <param name="exception">The exception representing the response format error.</param>
    /// <returns>
    /// An instance of the <see cref="QueryException"/> class containing information about the response format error.
    /// </returns>
    protected static QueryException GenerateQueryException(JsonException exception)
    {
        return new QueryException(FailureReason.UnknownResponseFormat, exception.Message);
    }

    /// <summary>
    /// Converts the given API-specific error into a corresponding query exception.
    /// </summary>
    /// <param name="error">The information about the API error.</param>
    /// <returns>
    /// An instance of the <see cref="QueryException"/> class containing information about the API-specific error.
    /// </returns>
    protected static QueryException GenerateQueryException(in ApiErrorInfo error)
    {
        switch (error.Code)
        {
            case -1002: // You are not authorized to execute this request.
                return new QueryException(FailureReason.Unauthorized, error.Message);
            case -1003: // Too many requests queued.
                return new QueryException(FailureReason.RateLimitViolated, error.Message);
            case -1006: // An unexpected response was received from the message bus. Execution status unknown.
            case -1007: // Timeout waiting for response from backend server.
                return new QueryException(FailureReason.BinanceInternalError);
            case -1021:
                return new QueryException(FailureReason.RequestOutdated, error.Message);
            case -2015: // Invalid API-key, IP, or permissions for action.
                return new QueryException(FailureReason.Unauthorized, error.Message);
            case -3000:
                return new QueryException(FailureReason.BinanceInternalError);
        }

        if (Enum.IsDefined(typeof(InputErrorCode), error.Code))
        {
            return new QueryInputException((InputErrorCode)error.Code);
        }

        return new QueryException(FailureReason.Other, $"{error.Code}: {error.Message}");
    }

    #endregion

    #region Instance methods

    // Initialization.
    /// <summary>
    /// Prepares the client for creating and sending queries, asynchronously.
    /// <para>An exact initialization procedure depends on a specific implementation of the client,
    /// but it typically includes validating API credentials and setting / updating API rate limits.</para>
    /// </summary>
    /// <param name="ct">The cancellation token for the operation.</param>
    /// <returns>A task representing the initialization operation.</returns>
    public Task InitializeAsync(CancellationToken ct)
    {
        ThrowIfDisposed();

        Task? t = _initializationTask;
        if (t != null)
        {
            return t;
        }

        t = _initializationTask = InitializePrivateAsync(ct);
        Task.Run(async () =>
        {
            try
            {
                await t.ConfigureAwait(false);
                if (IsLogLevelEnabled(LogLevel.Debug))
                    LogMessage(LogLevel.Debug, $"The initialization of the {GetType().Name} instance completed.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                LogMessage(LogLevel.Debug, $"The initialization of the {GetType().Name} instance was canceled.");
            }
            catch (Exception exc)
            {
                LogMessage(LogLevel.Error, $"The initialization of the {GetType().Name} instance failed:{exc}");
            }
            finally
            {
                if (_initializationTask == t)
                    _initializationTask = null;
            }
        }, ct);

        return t;
    }

    private async Task InitializePrivateAsync(CancellationToken ct)
    {
        await InitializeProtectedAsync(ct);
        if (_state == ClientState.Created)
            _state = ClientState.Running;
    }

    /// <summary>
    /// When implemented in a derived class, performs actions required for the client
    /// for being able to create &amp; send queries in its API area.
    /// <para>The initialization typically includes validating API credentials and setting / updating API rate limits.</para>
    /// </summary>
    /// <param name="ct">The cancellation token for the operation.</param>
    /// <returns>A task representing the initialization operation within a derived class.</returns>
    protected abstract Task InitializeProtectedAsync(CancellationToken ct);

    // Rate limiters.
    /// <summary>
    /// Checks the rate limits against a new query with the specified weights.
    /// <para>Throws <see cref="QueryException"/> if any of the rate limits is at risk.</para>
    /// </summary>
    /// <param name="weights">An array of extra weights to check for.</param>
    private void ThrowIfRateLimitsAtRisk(IReadOnlyList<QueryWeight> weights)
    {
        if (_limitsRegistry != null && !_limitsRegistry.TestUsage(weights, out int violatedLimitId))
        {
            RateLimitInfo limitInfo = _limitsRegistry[violatedLimitId];

            uint? amount = null;
            int count = weights.Count;
            for (int i = 0; i != count;)
            {
                QueryWeight w = weights[i++];
                if (w.DimensionId == limitInfo.DimensionId)
                {
                    amount = w.Amount;
                    break;
                }
            }

            string errorMessage = "The query was not executed to prevent violation of the rate limit " +
                $"{limitInfo.Name} (current usage: {limitInfo.Usage}, query weight: {amount}, limit: {limitInfo.Limit})";
            throw new QueryException(FailureReason.RateLimitPrevention, errorMessage);
        }
    }

    private void UpdateRateLimitsUsage(List<KeyValuePair<string, string>> limitsUsage,
        IReadOnlyDictionary<string, int> headersToLimitsMap, DateTime timestamp)
    {
        foreach (KeyValuePair<string, string> pair in limitsUsage)
        {
            if (headersToLimitsMap.TryGetValue(pair.Key, out int limitId))
            {
                if (uint.TryParse(pair.Value, out uint usageLevel))
                {
                    _limitsRegistry.UpdateUsage(limitId, usageLevel, timestamp);
                }
                else
                {
                    RateLimitInfo limitInfo = _limitsRegistry[limitId];
                    LogMessage(
                        LogLevel.Warning,
                        $"The usage value \"{pair.Value}\" for the limit {limitId} ({limitInfo.Name}) cannot be parsed.");
                }
            }
            else
            {
                LogMessage(LogLevel.Warning,
                    $"Cannot determine the rate limit by the header name \"{pair.Key}\". " +
                    $"Perhaps the headers-limits map is incomplete ({headersToLimitsMap.Count} items).");
            }
        }
    }

    // Query execution.
    /// <summary>
    /// Executes a web query asynchronously with the specified parameters, handling API rate limits and parsing the response.
    /// </summary>
    /// <typeparam name="T">The type of the parsed response.</typeparam>
    /// <param name="queryParams">The parameters defining the query.</param>
    /// <param name="weights">The rate limit weights associated with the query.</param>
    /// <param name="parseFunction">The function to use for parsing the response.</param>
    /// <param name="parseArgs">Additional arguments to pass into the parse function.</param>
    /// <param name="headersToLimitsMap">The HTTP headers map to use for updating the rate limits usage.</param>
    /// <param name="ct">The cancellation token for the operation.</param>
    /// <returns>A task representing the execution of the web query. Its result is the parsed web response.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="QueryException"/>
    internal async Task<T> ExecuteQueryAsync<T>(
        QueryParams queryParams,
        IReadOnlyList<QueryWeight> weights,
        ParseResponseHandler<T> parseFunction,
        object? parseArgs,
        IReadOnlyDictionary<string, int>? headersToLimitsMap,
        CancellationToken ct)
    {
        if (queryParams.IsUndefined)
        {
            throw new ArgumentNullException(nameof(queryParams));
        }
        if (parseFunction == null)
        {
            throw new ArgumentNullException(nameof(parseFunction));
        }

        // Check the rate limits.
        ct.ThrowIfCancellationRequested();
        ThrowIfRateLimitsAtRisk(weights);

        // Send the query.
        DateTime timestamp = Connector.TimeProvider.UtcNow;
        Task<Response> rsp = Connector.SendAsync(queryParams, ct);

        // Register rate limits usage.
        _limitsRegistry.IncrementUsage(weights, timestamp);

        // Wait for the response.
        await rsp.ConfigureAwait(false);

        // Update the current rate limits usage.
        ct.ThrowIfCancellationRequested();
        if (headersToLimitsMap != null && headersToLimitsMap.Count != 0)
        {
            if (rsp.Result.LimitsUsage.Count != 0)
            {
                UpdateRateLimitsUsage(rsp.Result.LimitsUsage, headersToLimitsMap, timestamp);
            }
            else if (rsp.Result.IsSuccessful)
            {
                LogMessage(LogLevel.Warning,
                    $"The web response contains the empty rate limit metrics. Perhaps the limit metrics map " +
                    $"has not been registered for the endpoint \"{queryParams.RelativeEndpoint}\".");
            }
        }
#if DEBUG
        if (_logger != null && _limitsRegistry is RateLimitsRegistry rlr)
        {
            ((RateLimitsRegistry)_limitsRegistry).LogCurrentUsage(_logger);
        }
#endif

        // Parse the response content.
        if (rsp.Result.IsSuccessful)
        {
            try
            {
                return parseFunction(rsp.Result.Content, parseArgs);
            }
            catch (JsonException exc)
            {
                throw new QueryException(FailureReason.UnknownResponseFormat, exc.ToString());
            }
        }
        else
        {
            if (ParseUtility.TryParseErrorInfo(rsp.Result.Content, out ApiErrorInfo error))
            {
                throw GenerateQueryException(in error);
            }
            else
            {
                throw new QueryException(FailureReason.Other, CommonUtility.DecodeByteContent(rsp.Result.Content));
            }
        }

    }

    // Miscellaneous.
    /// <summary>
    /// Checks whether the given log severity level is enabled in the client's logger.
    /// </summary>
    /// <param name="level">The log severity level to check.</param>
    /// <returns><see langword="true"/> if the client's logger accepts messages with the specified log severity level;
    /// otherwise, <see langword="false"/>.</returns>
    protected bool IsLogLevelEnabled(LogLevel level)
    {
        return _logger != null && _logger.IsEnabled(level);
    }

    /// <summary>
    /// Pushes a message into the client's logger at the specified log level.
    /// </summary>
    /// <param name="level">The log severity level of the message.</param>
    /// <param name="message">The message to be logged.</param>
    protected void LogMessage(LogLevel level, string message)
    {
        _logger?.Log(level, "{Message}", message);
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if <see cref="IsDisposed"/> is <see langword="true"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    protected void ThrowIfDisposed()
    {
        if (_state == ClientState.Disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>
    /// Throws an exception if the client is either not initialized or disposed.
    /// <para>The exact type of the exception thrown depends on the values of <see cref="IsInitialized"/> and <see cref="IsDisposed"/>.</para>
    /// </summary>
    /// <exception cref="ClientNotInitializedException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="Exception"/>
    protected void ThrowIfNotRunning()
    {
        if (_state != ClientState.Running)
        {
            throw _state switch
            {
                ClientState.Created => new ClientNotInitializedException(this),
                ClientState.Disposed => new ObjectDisposedException(GetType().Name),
                _ => new Exception($"The {GetType().Name} instance is in the invalid state."),
            };
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool releaseManaged)
    {
        if (_state == ClientState.Disposed) return;

        // Set in the beginning to prevent any asynchronous usage of the class before the disposal completes.
        _state = ClientState.Disposed;

        try
        {
            OnDisposing(true);
        }
        catch (Exception exc)
        {
            LogMessage(LogLevel.Error, $"The disposal of the {GetType().Name} instance failed:\r\n{exc}");
        }

        if (releaseManaged)
        {
            _limitsRegistry = null!;
            _connector = null!;
            _logger = null;
        }
    }

    /// <summary>
    /// Invoked from the <see cref="Dispose"/> method before any resources are released by the base class.
    /// </summary>
    /// <param name="releaseManaged">Indicates whether managed resources should be released.</param>
    protected virtual void OnDisposing(bool releaseManaged) { }

    #endregion

    #region Destructor

    ~ApiClientBase()
    {
        Dispose(false);
    }

    #endregion
}