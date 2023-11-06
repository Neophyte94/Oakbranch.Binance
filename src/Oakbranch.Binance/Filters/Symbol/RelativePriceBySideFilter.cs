using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// A filter that defines valid range for a price based on the average of the previous trades.
    /// </summary>
    public sealed class RelativePriceBySideFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.RelativePriceBySide;

        public decimal BidMultiplierUp;
        public decimal BidMultiplierDown;
        public decimal AskMultiplierUp;
        public decimal AskMultiplierDown;
        /// <summary>
        /// Defines the number of minutes the average price is calculated over. Null means the last price is used.
        /// </summary>
        public uint? AvgPriceInterval;
    }
}
