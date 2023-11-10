using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the quantity (aka "lots" in auction terms) rules for a symbol. 
    /// </summary>
    public sealed record LotSizeFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.LotSize;

        /// <summary>
        /// Defines the minimum quantity/icebergQty allowed.
        /// </summary>
        public decimal MinQuantity;
        /// <summary>
        /// Defines the maximum quantity/icebergQty allowed.
        /// </summary>
        public decimal MaxQuantity;
        /// <summary>
        /// Defines the intervals that a quantity/icebergQty can be increased/decreased by.
        /// </summary>
        public decimal StepSize;
    }
}