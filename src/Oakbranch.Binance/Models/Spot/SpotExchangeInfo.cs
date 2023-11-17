using System;
using System.Collections.Generic;
using Oakbranch.Binance.Models;

namespace Oakbranch.Binance.Models.Spot
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
