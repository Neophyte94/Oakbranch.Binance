using System;
using System.Collections.Generic;
using Oakbranch.Binance.Models;

namespace Oakbranch.Binance.Models.Futures
{
    /// <summary>
    /// Represents information on the configuration and state of the futures exchange.
    /// </summary>
    public sealed record FuturesExchangeInfo : ExchangeInfoBase
    {
        /// <summary>
        /// Defines the list of assets available on the exchange.
        /// <para>The value is <c>Null</c> if it is not applicable to the exchange.</para>
        /// </summary>
        public List<AssetInfo>? Assets;
        /// <summary>
        /// Defines the list of symbols available on the exchange.
        /// </summary>
        public List<SymbolInfo>? Symbols;
    }
}
