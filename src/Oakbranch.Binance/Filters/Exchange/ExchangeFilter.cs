using System;

namespace Oakbranch.Binance.Filters.Exchange
{
    /// <summary>
    /// The base class for exchange-level filters (contraints).
    /// </summary>
    public abstract record ExchangeFilter
    {
        /// <summary>
        /// Gets the type of the exchange filter.
        /// </summary>
        public abstract ExchangeFilterType Type { get; }
    }
}
