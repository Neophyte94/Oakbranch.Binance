using System;
using System.Collections.Generic;
using System.Linq;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.RateLimits;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Provides common functionality for API client classes using endpoints that share a single set of rate limits.
    /// <para>This class implements logic for registering / updating rate limits and provides the headers to limits map.</para>
    /// </summary>
    public abstract class SharedLimitsApiClientBase : ApiClientBase
    {
        #region Instance members

        /// <summary>
        /// Defines the endpoint used for identifying all rate limits used by the <see cref="SharedLimitsApiClientBase"/>'s endpoints.
        /// </summary>
        protected readonly string DiscrimativeEndpoint;
        /// <summary>
        /// Defines the string used in limits' names to reflect the discriminative endoint.
        /// </summary>
        private readonly string LimitNameEndpointSpecifier;

        private readonly int[] m_WeightDimensions;

        private readonly Dictionary<string, int> m_HeadersToLimitsMap;
        protected IReadOnlyDictionary<string, int> HeadersToLimitsMap
        {
            get
            {
                ThrowIfNotRunning();
                return m_HeadersToLimitsMap;
            }
        }

        #endregion

        #region Instance constructors

        /// <summary>
        /// Creates an instance of the <see cref="SharedLimitsApiClientBase"/> class with the specified parameters.
        /// </summary>
        /// <param name="connector">The API connector to use for executing web requests.</param>
        /// <param name="limitsRegistry">The registry of the rate limits to use for tracking limits usage.</param>
        /// <param name="discriminativeEndpoint">The identifying endpoint for all the rate limits used by the client's endpoints.</param>
        /// <param name="logger">The logger to use for posting runtime information (optional).</param>
        public SharedLimitsApiClientBase(
            IApiConnector connector,
            IRateLimitsRegistry limitsRegistry,
            string discriminativeEndpoint,
            ILogger? logger = null) :
            base(connector, limitsRegistry, logger)
        {
            if (String.IsNullOrWhiteSpace(discriminativeEndpoint))
                throw new ArgumentNullException(nameof(discriminativeEndpoint));

            DiscrimativeEndpoint = discriminativeEndpoint;
            LimitNameEndpointSpecifier = discriminativeEndpoint.Split('/')
                .First((n) => !String.IsNullOrWhiteSpace(n))
                .ToUpperInvariant();

            int[] limitTypes = (int[])Enum.GetValues(typeof(RateLimitType));
            m_WeightDimensions = new int[limitTypes.Length];
            for (int i = 0; i != limitTypes.Length; ++i)
            {
                m_WeightDimensions[i] = GenerateWeightDimensionId(DiscrimativeEndpoint, (RateLimitType)i);
            }

            m_HeadersToLimitsMap = new Dictionary<string, int>(4);
        }

        #endregion

        #region Instance methods

        /// <summary>
        /// When implemented, determines a name of the HTTP header that (presumably) contains
        /// info on the corresponding rate limit usage.
        /// </summary>
        /// <param name="limit">The specification of the limit to get header name for.</param>
        /// <returns>The name of the HTTP header containing info on the corresponding rate limit usage.</returns>
        protected abstract string GetRateLimitHeaderName(RateLimiter limit);

        protected void RegisterOrUpdateRateLimits(IList<RateLimiter> limits)
        {
            Dictionary<string, int> headersLimitsMap = new Dictionary<string, int>(limits.Count);

            foreach (RateLimiter limit in limits)
            {
                if (limit.Type == RateLimitType.RawRequests)
                {
                    // The raw request limits are considered obsolete.
                    continue;
                }

                int dimId = GetWeightDimensionId(limit.Type);
                TimeSpan interval = CommonUtility.CreateTimespan(limit.Interval, limit.IntervalNumber);
                int limitId = GenerateRateLimitId(dimId, interval);
                bool wasRegistered = false;

                if (!LimitsRegistry.ContainsLimit(limitId))
                {
                    string limitName = $"{LimitNameEndpointSpecifier} {limit.Type} {CommonUtility.GetIntervalDescription(interval)}";
                    RateLimitInfo limitInfo = new RateLimitInfo(dimId, interval, limit.Limit, 0, limitName);
                    wasRegistered = LimitsRegistry.TryRegisterLimit(limitId, limitInfo);
                }
                if (!wasRegistered)
                {
                    LimitsRegistry.ModifyLimit(limitId, limit.Limit);
                }

                string headerName = GetRateLimitHeaderName(limit);
                if (headerName != null)
                    headersLimitsMap.Add(headerName, limitId);
            }

            // Update the dictionary of the HTTP headers and the corresponding rate limits.
            m_HeadersToLimitsMap.Clear();
            foreach (KeyValuePair<string, int> pair in headersLimitsMap)
            {
                m_HeadersToLimitsMap.Add(pair.Key, pair.Value);
            }
            // We don't check whether the limit metrics map is already registered because we want to update it.
            Connector.SetLimitMetricsMap(DiscrimativeEndpoint, headersLimitsMap.Keys.ToArray());
        }

        /// <summary>
        /// Gets an identifier of a limit weight on spot API endpoints for the specified limit type.
        /// </summary>
        protected int GetWeightDimensionId(RateLimitType limitType)
        {
            return m_WeightDimensions[(int)limitType];
        }

        #endregion
    }
}
