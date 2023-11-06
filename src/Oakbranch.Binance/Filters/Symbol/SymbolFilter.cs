using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// The base class for filters (constraints) applied to a trading symbol.
    /// </summary>
    public abstract class SymbolFilter
    {
        /// <summary>
        /// Gets the type of the symbol filter.
        /// </summary>
        public abstract SymbolFilterType Type { get; }
    }
}