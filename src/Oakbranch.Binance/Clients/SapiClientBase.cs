using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Utility;
using Oakbranch.Binance.Models;
using Oakbranch.Binance.Models.Margin;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Core.RateLimits;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Clients
{
    /// <summary>
    /// Provides common functionality for API client classes using "/sapi/v1" endpoints.
    /// </summary>
    public abstract class SapiClientBase : ApiClientBase
    {
        #region Constants

        private const string GetSystemStatusEndpoint = "/sapi/v1/system/status";

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

        /// <summary>
        /// Defines the list containing templates of all rate limits relevant to SAPI endpoints.
        /// <para>New rate limits are created from these templates for each separate discriminative endpoint.</para>
        /// </summary>
        private readonly List<RateLimiter> _limitTemplates;
        /// <summary>
        /// Defines the dictionary of limit IDs keyed by their weight dimensions' IDs. 
        /// </summary>
        private readonly Dictionary<int, int[]> _dimensionsToLimitsDict;
        /// <summary>
        /// Defines teh dictionary of limit headers names keyed by their discriminative endpoints.
        /// </summary>
        private readonly Dictionary<string, ReadOnlyDictionary<string, int>> _headersToLimitsMapsDict;

        #endregion

        #region Static constructor

        static SapiClientBase()
        {
            s_RESTBaseEndpoints = new ReadOnlyCollection<BaseEndpoint>(new BaseEndpoint[]
            {
                new BaseEndpoint(NetworkType.Live, "https://api.binance.com", "API live network 1"),
                new BaseEndpoint(NetworkType.Live, "https://api1.binance.com", "API live network 2"),
                new BaseEndpoint(NetworkType.Live, "https://api2.binance.com", "API live network 3"),
                new BaseEndpoint(NetworkType.Live, "https://api3.binance.com", "API live network 4"),
                new BaseEndpoint(NetworkType.Live, "https://api4.binance.com", "API live network 5"),
            });
        }

        #endregion

        #region Instance constructors

        public SapiClientBase(IApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger? logger = null)
            : base(connector, limitsRegistry, logger)
        {
            _RESTEndpoint = s_RESTBaseEndpoints.First((bep) => bep.Type == NetworkType.Live);
            _limitTemplates = new List<RateLimiter>(4);
            _dimensionsToLimitsDict = new Dictionary<int, int[]>(64);
            _headersToLimitsMapsDict = new Dictionary<string, ReadOnlyDictionary<string, int>>(
                64, StringComparer.InvariantCultureIgnoreCase);
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Gets an identifier of a limit weight on spot SAPI endpoints for the specified limit type.
        /// </summary>
        /// <param name="discriminativeEndpoint">The endpoint that all limits targeting this weight dimension are bound to.</param>
        /// <param name="limitType">The limit type to get the weight dimension for.</param>
        protected static int GetWeightDimensionId(string discriminativeEndpoint, RateLimitType limitType)
        {
            return GenerateWeightDimensionId(discriminativeEndpoint, limitType);
        }

        protected static string? GetHeaderName(RateLimiter limiter)
        {
            return limiter.Type switch
            {
                RateLimitType.UID => $"X-SAPI-USED-UID-WEIGHT-{limiter.IntervalNumber}{Format(limiter.Interval)}",
                RateLimitType.IP => $"X-SAPI-USED-IP-WEIGHT-{limiter.IntervalNumber}{Format(limiter.Interval)}",
                RateLimitType.RawRequests => null,
                _ => throw new ArgumentException($"An unknown rate limit type \"{limiter.Type}\" was specified."),
            };
        }

        #endregion

        #region Instance methods

        // Initialization.
        protected override async Task InitializeProtectedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Send the initialization query.
            QueryParams initQueryParams = new QueryParams(
                HttpMethod.GET, _RESTEndpoint.Url, GetSystemStatusEndpoint, null, false);
            Response rsp = await Connector.SendAsync(initQueryParams, ct).ConfigureAwait(false);

            // Parse the response.
            EnsureSuccessfulInitResponse(rsp);
            SystemStatus status = ParseSystemStatus(rsp.Content, null);

            // Check the server status.
            if (status != SystemStatus.Normal)
            {
                LogMessage(LogLevel.Warning, $"The reported status of the Binance server is \"{status}\".");
            }

            // Register or update SAPI rate limits in the registry.
            _limitTemplates.Clear();
            _limitTemplates.Add(new RateLimiter(RateLimitType.IP, Interval.Minute, 1, 12000));
            _limitTemplates.Add(new RateLimiter(RateLimitType.UID, Interval.Minute, 1, 180000));
        }

        // Rate limits.
        /// <summary>
        /// Registers rate limits for the specified SAPI endpoint and limit type, or skips of they already exist.
        /// </summary>
        /// <param name="discriminativeEndpoint">The endpoint to register or update rate limits for.</param>
        /// <param name="limitType">The limits type that the endpoint consumes.</param>
        protected void RegisterRateLimitsIfNotExist(string discriminativeEndpoint, RateLimitType limitType)
        {
            ThrowIfNotRunning();
            if (string.IsNullOrWhiteSpace(discriminativeEndpoint))
                throw new ArgumentNullException(nameof(discriminativeEndpoint));

            // Check whether rate limits have been registered for the specified "endpoint - limit type" pair.
            int dimId = GetWeightDimensionId(discriminativeEndpoint, limitType);
            if (AreRateLimitsRegistered(dimId)) return;

            // Create a buffer for the headers' names to limits' IDs map.
            Dictionary<string, int> headersLimitsMap = new Dictionary<string, int>(4, StringComparer.InvariantCultureIgnoreCase);

            // Traverse through the limit templates, and create limits corresponding to the specified limit type.
            foreach (RateLimiter template in _limitTemplates)
            {
                if (template.Type == limitType)
                {
                    TimeSpan interval = CommonUtility.CreateTimespan(template.Interval, template.IntervalNumber);
                    int limitId = GenerateRateLimitId(dimId, interval);
                    bool wasRegistered = false;

                    if (!LimitsRegistry.ContainsLimit(limitId))
                    {
                        RateLimitInfo limitInfo = new RateLimitInfo(dimId, interval, template.Limit, 0,
                            $"SAPI {template.Type} {CommonUtility.GetIntervalDescription(interval)} {discriminativeEndpoint}");
                        wasRegistered = LimitsRegistry.TryRegisterLimit(limitId, limitInfo);
                    }
                    if (!wasRegistered)
                    {
                        LimitsRegistry.ModifyLimit(limitId, template.Limit);
                    }

                    string? headerName = GetHeaderName(template);
                    if (headerName != null)
                    {
                        headersLimitsMap[headerName] = limitId;
                    }
                }
            }

            // Update the dictionary of limits IDs keyed by weight dimensions.
            _dimensionsToLimitsDict[dimId] = headersLimitsMap.Values.ToArray();

            // Update the headers names to limits IDs map.
            if (_headersToLimitsMapsDict.TryGetValue(discriminativeEndpoint, out ReadOnlyDictionary<string, int>? existingMap))
            {
                foreach (KeyValuePair<string, int> pair in existingMap)
                {
                    if (!headersLimitsMap.ContainsKey(pair.Key))
                        headersLimitsMap.Add(pair.Key, pair.Value);
                }
            }
            _headersToLimitsMapsDict[discriminativeEndpoint] = new ReadOnlyDictionary<string, int>(headersLimitsMap);
            Connector.SetLimitMetricsMap(discriminativeEndpoint, headersLimitsMap.Keys.ToArray());
        }

        /// <summary>
        /// Checks whether <see cref="ApiClientBase.LimitsRegistry"/> has all limits registered for the specified weight dimension.
        /// </summary>
        private bool AreRateLimitsRegistered(int weightDimensionId)
        {
            if (_dimensionsToLimitsDict.TryGetValue(weightDimensionId, out int[]? associatedLimits))
            {
                foreach (int limitId in associatedLimits)
                {
                    if (!LimitsRegistry.ContainsLimit(limitId))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the headers names to limits IDs map for the specified endpoint.
        /// <para>Throws <see cref="InvalidOperationException"/> if the rate limits have not been registered yet.</para>
        /// </summary>
        /// <param name="discriminativeEndpoint">The endpoint to get the map for.</param>
        protected IReadOnlyDictionary<string, int> GetHeadersToLimitsMap(string discriminativeEndpoint)
        {
            ThrowIfNotRunning();

            if (_headersToLimitsMapsDict.TryGetValue(discriminativeEndpoint, out ReadOnlyDictionary<string, int>? dict))
            {
                return dict;
            }
            else
            {
                throw new InvalidOperationException(
                    $"A header to limits map has not been created for the endpoint {discriminativeEndpoint}. " +
                    $"Ensure to call {nameof(RegisterRateLimitsIfNotExist)}() before requesting the map.");
            }
        }

        // Get system status.
        /// <summary>
        /// Prepares a query for the current system status.
        /// </summary>
        public IDeferredQuery<SystemStatus> PrepareGetSystemStatus()
        {
            ThrowIfNotRunning();

            string relEndpoint = GetSystemStatusEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 1),
            };

            return new DeferredQuery<SystemStatus>(
                query: new QueryParams(HttpMethod.POST, RESTEndpoint.Url, relEndpoint, null, false),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseSystemStatus,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        private SystemStatus ParseSystemStatus(byte[] data, object? _)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);

            int? statusCode = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                if (!reader.Read())
                {
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                }

                switch (propName)
                {
                    case "status":
                        statusCode = reader.GetInt32();
                        break;
                    case "msg":
                        // The status description is not stored.
                        reader.Skip();
                        break;
                    default:
                        LogMessage(LogLevel.Warning, $"An unknown system status property \"{propName}\" was encountered.");
                        reader.Skip();
                        break;
                }
            }

            if (statusCode == null)
            {
                throw ParseUtility.GenerateMissingPropertyException("system status", "status code");
            }

            return statusCode.Value switch
            {
                0 => SystemStatus.Normal,
                1 => SystemStatus.Maintenance,
                _ => throw new JsonException($"An unknown system status code \"{statusCode}\" was encountered."),
            };
        }

        #endregion
    }
}
