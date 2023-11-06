using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Represents information on the state of an isolated margin account on a specific symbol.
    /// </summary>
    public sealed class IsolatedSymbolAccInfo
    {
        /// <summary>
        /// Defines the symbol associated with the account.
        /// </summary>
        public string Symbol;
        /// <summary>
        /// Defines the base asset information for the account.
        /// </summary>
        public IsolatedAsset BaseAsset;
        /// <summary>
        /// Defines the quote asset information for the account.
        /// </summary>
        public IsolatedAsset QuoteAsset;
        /// <summary>
        /// Defines whether the account has been created.
        /// </summary>
        public bool IsCreated;
        /// <summary>
        /// Defines whether the account is enabled.
        /// </summary>
        public bool IsEnabled;
        /// <summary>
        /// Defines whether trading is enabled for the account.
        /// </summary>
        public bool IsTradeEnabled;
        /// <summary>
        /// Defines the current margin level of the account.
        /// </summary>
        public double MarginLevel;
        /// <summary>
        /// Defines the status of the margin level of the account.
        /// </summary>
        public MarginStatus MarginLevelStatus;
        /// <summary>
        /// Defines that maximum margin level supported in the isolated margin symbol.
        /// <para>For example, 5.0x or 10.0x.</para>
        /// </summary>
        public double MarginRatio;
        /// <summary>
        /// Defines the current index price of the symbol.
        /// </summary>
        public double IndexPrice;
        /// <summary>
        /// Defines the price at which the account will be liquidated.
        /// </summary>
        public double LiquidatePrice;
        /// <summary>
        /// Defines the liquidation rate of the account.
        /// </summary>
        public double LiquidateRate;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => $"Isolated {Symbol}: Ratio = {MarginRatio}, Margin level = {MarginLevel}";
    }
}
