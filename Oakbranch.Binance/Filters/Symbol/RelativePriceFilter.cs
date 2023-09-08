using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// A filter that defines valid range for a price based on the average of the previous trades.
    /// </summary>
    public sealed class RelativePriceFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.RelativePrice;

        public decimal MultiplierUp;
        public decimal MultiplierDown;
        /// <summary>
        /// Defines the number of minutes the average price is calculated over.
        /// <para>The <c>Null</c> value means the last price is used.</para> 
        /// </summary>
        public uint? AvgPriceInterval;
    }
}
