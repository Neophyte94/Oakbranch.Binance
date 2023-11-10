using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Spot
{
    /// <summary>
    /// Represents information on the configuration and state of the spot exchange.
    /// </summary>
    public sealed record SpotExchangeInfo : ExchangeInfoBase
    {
        /// <summary>
        /// Defines the list of symbols available on the exchange.
        /// </summary>
        public List<SymbolInfo>? Symbols;
    }
}
