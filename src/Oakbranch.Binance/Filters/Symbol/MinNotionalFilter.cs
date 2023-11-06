using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the minimum notional value allowed for an order on a symbol.
    /// <para>An order's notional value is the price * quantity.</para>
    /// </summary>
    public sealed class MinNotionalFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.MinNotional;

        public decimal MinNotional;
        /// <summary>
        /// Defines whether the filter applies to market orders.
        /// </summary>
        public bool DoesApplyToMarket = true;
        /// <summary>
        /// Defines the number of minutes the average price is calculated over.
        /// <para>The <c>Null</c> value means the last price (spot) or mark price (futures) is used.</para>
        /// <para>Average price is only used if <see cref="DoesApplyToMarket"/> is <see langword="true"/>.</para>
        /// </summary>
        public uint? AvgPriceInterval;
    }
}
