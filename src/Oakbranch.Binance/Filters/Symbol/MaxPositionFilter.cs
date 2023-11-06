using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the allowed maximum position an account can have on the base asset of a symbol.
    /// <para>Buy orders will be rejected if the account's position is greater than the maximum position allowed.</para>
    /// </summary>
    public sealed class MaxPositionFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.MaxPosition;

        public double MaxPosition;
    }
}
