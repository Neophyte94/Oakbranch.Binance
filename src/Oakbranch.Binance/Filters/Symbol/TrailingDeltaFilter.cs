using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the quantity (aka "lots" in auction terms) rules for a symbol. 
    /// </summary>
    public sealed record TrailingDeltaFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.TrailingDelta;

        public uint MinTrailingAboveDelta;
        public uint MaxTrailingAboveDelta;
        public uint MinTrailingBelowDelta;
        public uint MaxTrailingBelowDelta;
    }
}