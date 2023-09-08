using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Represents summary on all isolated margin accounts.
    /// </summary>
    public sealed class IsolatedAccountsInfo
    {
        /// <summary>
        /// Defines a list of symbol-specific information in the isolated margin account.
        /// </summary>
        public List<IsolatedSymbolAccInfo> IsolatedSymbols;
        /// <summary>
        /// Defines the total value of all owned assets in the account, denominated in Bitcoin (BTC).
        /// </summary>
        public double? TotalAssetOfBTC;
        /// <summary>
        /// Defines the total value of the liabilities in the account, denominated in Bitcoin (BTC).
        /// </summary>
        public double? TotalLiabilityOfBTC;
        /// <summary>
        /// Defines the total net asset value of the account, denominated in Bitcoin (BTC).
        /// </summary>
        public double? TotalNetAssetOfBTC;
    }
}
