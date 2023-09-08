using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Defines a margin trading pair.
    /// </summary>
    public struct MarginPair
    {
        /// <summary>
        /// Defines the identifier of the margin trading pair.
        /// <para>The value is only defined for cross margin pairs. For isolated margin pairs the value is -1.</para>
        /// </summary>
        public long Id;
        /// <summary>
        /// Defines the symbol associated with the margin trading pair.
        /// </summary>
        public string Symbol;
        /// <summary>
        /// Defines the base asset of the margin trading pair.
        /// </summary>
        public string BaseAsset;
        /// <summary>
        /// Defines the quote asset of the margin trading pair.
        /// </summary>
        public string QuoteAsset;
        /// <summary>
        /// Defines whether buying is allowed for the  pair.
        /// </summary>
        public bool IsBuyAllowed;
        /// <summary>
        /// Defines whether selling is allowed for the pair.
        /// </summary>
        public bool IsSellAllowed;
        /// <summary>
        /// Defines whether margin trading is allowed for the pair.
        /// </summary>
        public bool IsMarginTrade;
    }
}
