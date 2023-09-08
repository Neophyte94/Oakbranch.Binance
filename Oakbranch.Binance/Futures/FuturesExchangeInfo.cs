using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Futures
{
    /// <summary>
    /// Represents information on the configuration and state of the futures exchange.
    /// </summary>
    public sealed class FuturesExchangeInfo : ExchangeInfoBase
    {
        /// <summary>
        /// Defines the list of assets available on the exchange.
        /// <para>The value is <c>Null</c> if it is not applicable to the exchange.</para>
        /// </summary>
        public List<AssetInfo> Assets;
        /// <summary>
        /// Defines the list of symbols available on the exchange.
        /// </summary>
        public List<SymbolInfo> Symbols;
    }
}
