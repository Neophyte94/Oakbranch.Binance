using System;
using System.Collections.Generic;
using Oakbranch.Binance.Filters.Exchange;

namespace Oakbranch.Binance
{
    /// <summary>
    /// The base class for representing information on configuration and state of an exchange.
    /// </summary>
    public abstract record ExchangeInfoBase
    {
        /// <summary>
        /// Defines the time zone information for the exchange.
        /// </summary>
        public TimeZoneInfo? Timezone;
        /// <summary>
        /// Defines the current server time of the exchange.
        /// </summary>
        public DateTime ServerTime;
        /// <summary>
        /// Defines the list of rate limits on API calls.
        /// </summary>
        public List<RateLimiter>? RateLimits;
        /// <summary>
        /// Defines additional exchange-level filters (restrictions).
        /// </summary>
        public List<ExchangeFilter>? ExchangeFilters;
    }
}
