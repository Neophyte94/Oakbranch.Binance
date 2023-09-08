using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.Exceptions;
using Oakbranch.Binance.RateLimits;
using System.Text;

namespace Oakbranch.Binance
{
    public abstract class ApiClientBase : IDisposable
    {
        #region Constants

        protected const string LogContextNameDefault = "Binance API client";

        #endregion

        #region Instance members

        private IApiConnector m_Connector;
        public IApiConnector Connector
        {
            get
            {
                ThrowIfDisposed();
                return m_Connector;
            }
        }

        private IRateLimitsRegistry m_LimitsRegistry;
        public IRateLimitsRegistry LimitsRegistry => m_LimitsRegistry;

        private ILogger m_Logger;
        private Task m_InitializationTask;

        protected virtual string LogContextName => LogContextNameDefault;

        private ClientState m_State;
        public bool IsInitialized => m_State == ClientState.Running;
        public bool IsDisposed => m_State == ClientState.Disposed;

        #endregion

        #region Instance constructors

        internal ApiClientBase(IApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger logger = null)
        {
            m_Connector = connector ?? throw new ArgumentNullException(nameof(connector));
            m_LimitsRegistry = limitsRegistry ?? throw new ArgumentNullException(nameof(limitsRegistry));
            m_Logger = logger;
        }

        #endregion

        #region Static methods

        protected static int GenerateWeightDimensionId(string discriminativeEndpoint, RateLimitType limitType)
        {
            return unchecked(
                -673990 * Common.Utility.CommonUtility.GetDeterministicHashCode(discriminativeEndpoint)
                + 307 * (int)limitType);
        }

        protected static int GenerateRateLimitId(int dimensionId, TimeSpan resetInterval)
        {
            return unchecked(3105 * dimensionId - 9494 * (int)resetInterval.Ticks);
        }

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
                    throw new NotImplementedException($"An unknown rate limit type was encountered: \"{s}\".");
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

        protected static QueryException GenerateQueryException(JsonException exception)
        {
            return new QueryException(FailureReason.UnknownResponseFormat, exception.Message);
        }

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
        public Task InitializeAsync(CancellationToken ct)
        {
            ThrowIfDisposed();

            Task t = m_InitializationTask;
            if (t != null) return t;

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
            });

            return t;
        }

        private async Task InitializePrivateAsync(CancellationToken ct)
        {
            await InitializeProtectedAsync(ct);
            if (m_State == ClientState.Created)
                m_State = ClientState.Running;
        }

        protected abstract Task InitializeProtectedAsync(CancellationToken ct);

        /// <summary>
        /// Checks whether the specified response to an initialization query is successful,
        /// and throws <see cref="Exception"/> if it is not.
        /// <para>For a failed response, tries to parse its content and include the error message in the exception.</para>
        /// </summary>
        /// <param name="initQueryResponse">The response to the initialization query.</param>
        protected void EnsureSuccessfulInitResponse(Response initQueryResponse)
        {
            if (!initQueryResponse.IsSuccessful)
            {
                if (initQueryResponse.Content == null)
                {
                    throw new Exception($"The initialization query failed for an unknown reason.");
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
        protected RateLimiter ParseRateLimiter(ref Utf8JsonReader reader)
        {
            ParseUtility.ValidateObjectStartToken(ref reader);

            ParseSchemaValidator validator = new ParseSchemaValidator(4);
            RateLimitType limitType = default;
            Interval interval = default;
            ushort intervalNum = default;
            uint limit = default;
            uint? usage = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                switch (propName)
                {
                    case "rateLimitType":
                        if (reader.TokenType != JsonTokenType.String)
                            throw ParseUtility.GenerateInvalidValueTypeException(propName, JsonTokenType.String, reader.TokenType);
                        limitType = ParseRateLimitType(reader.GetString());
                        validator.RegisterProperty(0);
                        break;

                    case "interval":
                        if (reader.TokenType != JsonTokenType.String)
                            throw ParseUtility.GenerateInvalidValueTypeException(propName, JsonTokenType.String, reader.TokenType);
                        interval = ParseInterval(reader.GetString());
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
        internal async Task<T> ExecuteQueryAsync<T>(
            QueryParams queryParams, IReadOnlyList<QueryWeight> weights,
            ParseResponseHandler<T> parseFunction, object parseArgs,
            IReadOnlyDictionary<string, int> headersToLimitsMap, CancellationToken ct)
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
        protected bool IsLogLevelEnabled(LogLevel level)
        {
            return m_Logger != null && m_Logger.IsLevelEnabled(level);
        }

        protected void PostLogMessage(LogLevel level, string message)
        {
            m_Logger?.Log(level, LogContextName, message);
        }

        protected void ThrowIfDisposed()
        {
            if (m_State == ClientState.Disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

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

        public void Dispose()
        {
            if (m_State == ClientState.Disposed) return;
            m_State = ClientState.Disposed;

            try { OnDisposing(); }
            catch (Exception exc)
            {
                PostLogMessage(LogLevel.Error, $"The disposal of the {GetType().Name} instance failed:\r\n{exc}");
            }

            m_LimitsRegistry = null;
            m_Logger = null;
            m_Connector = null;
        }

        protected virtual void OnDisposing() { }

        #endregion
    }
}