using System;

namespace Oakbranch.Binance.Models.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the price rules for a symbol.
    /// </summary>
    public sealed record AbsolutePriceFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.AbsolutePrice;

        /// <summary>
        /// Defines the minimum price/stopPrice allowed. Disabled on <c>Null</c>.
        /// </summary>
        public decimal? MinPrice;
        /// <summary>
        /// Defines the maximum price/stopPrice allowed. Disabled on <c>Null</c>.
        /// </summary>
        public decimal? MaxPrice;
        /// <summary>
        /// Defines the intervals that a price/stopPrice can be increased/decreased by. Disabled on <c>Null</c>.
        /// </summary>
        public decimal? TickSize;
    }
}
