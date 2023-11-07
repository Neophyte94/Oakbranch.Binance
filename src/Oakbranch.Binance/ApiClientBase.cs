using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.Exceptions;
using Oakbranch.Binance.RateLimits;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Provides base functionality for high-level client classes built upon specific Binance API areas.
    /// <para>Implements the <see cref="IDisposable"/> interface.</para>
    /// </summary>
    public abstract class ApiClientBase : IDisposable
    {
        #region Constants

        protected const string LogContextNameDefault = "Binance API client";

        #endregion

        #region Instance members

        private IApiConnector m_Connector;
        /// <summary>
        /// Gets the API connector used by the client for accessing low-level functions. 
        /// </summary>
        public IApiConnector Connector
        {
            get
            {
                ThrowIfDisposed();
                return m_Connector;
            }
        }

        private IRateLimitsRegistry m_LimitsRegistry;
        /// <summary>
        /// Gets the rate limits registry used by the client for checking and updating API rate limits.
        /// </summary>
        public IRateLimitsRegistry LimitsRegistry => m_LimitsRegistry;

        private ILogger? m_Logger;
        private Task? m_InitializationTask;

        /// <summary>
        /// Gets a string used for specifying the logging context of logs posted by this client.
        /// </summary>
        protected virtual string LogContextName => LogContextNameDefault;

        private ClientState m_State;
        /// <summary>
        /// Gets a value indicating whether the client has been initialized and not disposed yet.
        /// </summary>
        public bool IsInitialized => m_State == ClientState.Running;
        /// <summary>
        /// Gets a value indicating whether the client has been disposed.
        /// </summary>
        public bool IsDisposed => m_State == ClientState.Disposed;

        #endregion

        #region Instance constructors

        /// <summary>
        /// Creates an instance of the <see cref="ApiClientBase"/> class with the provided components.
        /// </summary>
        /// <param name="connector">The API connector to use for accessing low-level web functions.</param>
        /// <param name="limitsRegistry">The rate limits registry to use for checking and updating API rate limits.</param>
        /// <param name="logger">The logger to use for posting log messages.</param>
        /// <exception cref="ArgumentNullException"/>
        internal ApiClientBase(IApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger? logger = null)
        {
            m_Connector = connector ?? throw new ArgumentNullException(nameof(connector));
            m_LimitsRegistry = limitsRegistry ?? throw new ArgumentNullException(nameof(limitsRegistry));
            m_Logger = logger;
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Generates a deterministic identifier for a query weight dimension with the given parameters.
        /// </summary>
        /// <param name="discriminativeEndpoint">The endpoint that the query weight dimension is applied to.</param>
        /// <param name="limitType">Th limit type associated with the query weight dimension.</param>
        /// <returns></returns>
        protected static int GenerateWeightDimensionId(string discriminativeEndpoint, RateLimitType limitType)
        {
            return unchecked(
                -673990 * Common.Utility.CommonUtility.GetDeterministicHashCode(discriminativeEndpoint)
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
        /// Converts the given time unit into its laconic string representation.
        /// </summary>
        /// <param name="unit">The time interval unit to format.</param>
        /// <returns>The string representation of the specified time unit.</returns>
        /// <exception cref="ArgumentException"/>
        protected static string Format(Interval unit)
        {
            switch (unit)
            {
                case Interval.Minute:
                    return "M";
                case Interval.Second:
                    return "S";
                case Interval.Day:
                    return "D";
                default:
                    throw new ArgumentException();
            }
        }

        protected static string Format(OrderSide value)
        {
            if (value == OrderSide.Buy)
                return "BUY";
            else
                return "SELL";
        }

        private static RateLimitType ParseRateLimitType(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new ArgumentNullException(nameof(s));

            switch (s)
            {
                case "RAW_REQUESTS":
                    return RateLimitType.RawRequests;
                case "REQUEST_WEIGHT":
                    return RateLimitType.IP;
                case "ORDERS":
                    return RateLimitType.UID;
                default:
                    throw new JsonException($"An unknown rate limit type was encountered: \"{s}\".");
            }
        }

        private static Interval ParseInterval(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new ArgumentNullException(nameof(s));

            switch (s)
            {
                case "MINUTE":
                    return Interval.Minute;
                case "SECOND":
                    return Interval.Second;
                case "HOUR":
                    return Interval.Hour;
                case "DAY":
                    return Interval.Day;
                default:
                    throw new JsonException($"An unknown rate limit interval was encountered: \"{s}\".");
            }
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

            Task? t = m_InitializationTask;
            if (t != null)
            {
                return t;
            }

            t = m_InitializationTask = InitializePrivateAsync(ct);
            Task.Run(async () =>
            {
                try
                {
                    await t.ConfigureAwait(false);
                    if (IsLogLevelEnabled(LogLevel.Debug))
                        PostLogMessage(LogLevel.Debug, $"The initialization of the {GetType().Name} instance completed.");
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    PostLogMessage(LogLevel.Debug, $"The initialization of the {GetType().Name} instance was canceled.");
                }
                catch (Exception exc)
                {
                    PostLogMessage(LogLevel.Error, $"The initialization of the {GetType().Name} instance failed:{exc}");
                }
                finally
                {
                    if (m_InitializationTask == t)
                        m_InitializationTask = null;
                }
            }, ct);

            return t;
        }

        private async Task InitializePrivateAsync(CancellationToken ct)
        {
            await InitializeProtectedAsync(ct);
            if (m_State == ClientState.Created)
                m_State = ClientState.Running;
        }

        /// <summary>
        /// When implemented in a derived class, performs actions required for the client
        /// for being able to create &amp; send queries in its API area.
        /// <para>The initialization typically includes validating API credentials and setting / updating API rate limits.</para>
        /// </summary>
        /// <param name="ct">The cancellation token for the operation.</param>
        /// <returns>A task representing the initialization operation within a derived class.</returns>
        protected abstract Task InitializeProtectedAsync(CancellationToken ct);

        /// <summary>
        /// Checks whether the specified response to an initialization query is successful,
        /// and throws <see cref="Exception"/> if it is not.
        /// <para>For a failed response, tries to parse its content and include the error message in the exception.</para>
        /// </summary>
        /// <param name="initQueryResponse">The response to the initialization query.</param>
        /// <exception cref="Exception"/>
        protected void EnsureSuccessfulInitResponse(Response initQueryResponse)
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

        // Rate limiters.
        /// <summary>
        /// Parses a single rate limiter from the current position in the given JSON reader.
        /// </summary>
        /// <param name="reader">The JSON reader to parse from.</param>
        /// <returns>An instance of the <see cref="RateLimiter"/> struct parsed from JSON.</returns>
        /// <exception cref="JsonException"/>
        protected RateLimiter ParseRateLimiter(ref Utf8JsonReader reader)
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
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "limit type");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "interval unit");
                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "interval number");
                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "limit");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            // Create a rate limiter instance and return it.
            return new RateLimiter(limitType, interval, intervalNum, limit, usage);
        }

        /// <summary>
        /// Checks the rate limits against a new query with the specified weights.
        /// <para>Throws <see cref="QueryException"/> if any of the rate limits is at risk.</para>
        /// </summary>
        /// <param name="weights">An array of extra weights to check for.</param>
        private void ThrowIfRateLimitsAtRisk(IReadOnlyList<QueryWeight> weights)
        {
            if (m_LimitsRegistry != null && !m_LimitsRegistry.TestUsage(weights, out int violatedLimitId))
            {
                RateLimitInfo limitInfo = m_LimitsRegistry[violatedLimitId];

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
                        m_LimitsRegistry.UpdateUsage(limitId, usageLevel, timestamp);
                    }
                    else
                    {
                        RateLimitInfo limitInfo = m_LimitsRegistry[limitId];
                        PostLogMessage(
                            LogLevel.Warning,
                            $"The usage value \"{pair.Value}\" for the limit {limitId} ({limitInfo.Name}) cannot be parsed.");
                    }
                }
                else
                {
                    PostLogMessage(LogLevel.Warning,
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
                throw new ArgumentNullException(nameof(queryParams));
            if (parseFunction == null)
                throw new ArgumentNullException(nameof(parseFunction));

            // Check the rate limits.
            ct.ThrowIfCancellationRequested();
            ThrowIfRateLimitsAtRisk(weights);

            // Send the query.
            DateTime timestamp = Connector.TimeProvider.UtcNow;
            Task<Response> rsp = Connector.SendAsync(queryParams, ct);

            // Register rate limits usage.
            m_LimitsRegistry.IncrementUsage(weights, timestamp);

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
                    PostLogMessage(LogLevel.Warning,
                        $"The web response contains the empty rate limit metrics. Perhaps the limit metrics map " +
                        $"has not been registered for the endpoint \"{queryParams.RelativeEndpoint}\".");
                }
            }
#if DEBUG
            if (m_Logger != null && m_LimitsRegistry is RateLimitsRegistry rlr)
                ((RateLimitsRegistry)m_LimitsRegistry).LogCurrentUsage(m_Logger);
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
            return m_Logger != null && m_Logger.IsLevelEnabled(level);
        }

        /// <summary>
        /// Pushes a message into the client's logger at the specified log level,
        /// using <see cref="LogContextName"/> as the logging context.
        /// </summary>
        /// <param name="level">The log severity level of the message.</param>
        /// <param name="message">The message to be logged.</param>
        protected void PostLogMessage(LogLevel level, string message)
        {
            m_Logger?.Log(level, LogContextName, message);
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> if <see cref="IsDisposed"/> is <see langword="true"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException"/>
        protected void ThrowIfDisposed()
        {
            if (m_State == ClientState.Disposed)
                throw new ObjectDisposedException(GetType().Name);
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
            if (m_State != ClientState.Running)
            {
                switch (m_State)
                {
                    case ClientState.Created:
                        throw new ClientNotInitializedException(this);
                    case ClientState.Disposed:
                        throw new ObjectDisposedException(GetType().Name);
                    default:
                        throw new Exception($"The {GetType().Name} instance is in the invalid state.");
                }
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
            if (m_State == ClientState.Disposed) return;

            // Set in the beginning to prevent any asynchronous usage of the class before the disposal completes.
            m_State = ClientState.Disposed; 

            try
            {
                OnDisposing(true);
            }
            catch (Exception exc)
            {
                PostLogMessage(LogLevel.Error, $"The disposal of the {GetType().Name} instance failed:\r\n{exc}");
            }

            if (releaseManaged)
            {
                m_LimitsRegistry = null!;
                m_Connector = null!;
                m_Logger = null;
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
}