using System;

namespace Oakbranch.Binance.Models.Filters.Symbol
{
    /// <summary>
    /// A filter that defines defines the acceptable notional range allowed for an order on a symbol. 
    /// <para>An order's notional value is the price * quantity.</para>
    /// </summary>
    public sealed record NotionalRangeFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.NotionalRange;

        /// <summary>
        /// Defines the minimum notional value allowed for an order on a symbol.
        /// </summary>
        public decimal MinNotional;
        /// <summary>
        /// Defines whether the <see cref="MinNotional"/> contraint applies to market orders.
        /// </summary>
        public bool IsMinAppliedToMarket = true;
        /// <summary>
        /// Defines the maximum notional value allowed for an order on a symbol.
        /// </summary>
        public decimal MaxNotional;
        /// <summary>
        /// Defines whether the <see cref="MaxNotional"/> contraint applies to market orders.
        /// </summary>
        public bool IsMaxAppliedToMarket = true;
        /// <summary>
        /// Defines the number of minutes the average price is calculated over.
        /// <para>The <c>Null</c> value means the last price (spot) or mark price (futures) is used.</para>
        /// <para>Average price is only used if <see cref="DoesApplyToMarket"/> is <see langword="true"/>.</para>
        /// </summary>
        public uint? AvgPriceInterval;
    }
}
