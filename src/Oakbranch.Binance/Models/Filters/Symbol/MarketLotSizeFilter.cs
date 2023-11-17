using System;

namespace Oakbranch.Binance.Models.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the quantity (aka "lots" in auction terms) rules for market orders on a symbol. 
    /// </summary>
    public sealed record MarketLotSizeFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.MarketLotSize;

        /// <summary>
        /// Defines the minimum quantity allowed.
        /// </summary>
        public double MinQuantity;
        /// <summary>
        /// Defines the maximum quantity allowed.
        /// </summary>
        public double MaxQuantity;
        /// <summary>
        /// Defines the intervals that a quantity can be increased/decreased by.
        /// </summary>
        public double StepSize;
    }
}