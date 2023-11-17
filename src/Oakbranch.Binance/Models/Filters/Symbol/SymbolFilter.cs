using System;

namespace Oakbranch.Binance.Models.Filters.Symbol
{
    /// <summary>
    /// The base class for filters (constraints) applied to a trading symbol.
    /// </summary>
    public abstract record SymbolFilter
    {
        /// <summary>
        /// Gets the type of the symbol filter.
        /// </summary>
        public abstract SymbolFilterType Type { get; }
    }
}