using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the maximum number of orders an account is allowed to have open on a symbol. 
    /// <para> Note that both "algo" orders and normal orders are counted for this filter.</para>
    /// </summary>
    public sealed record OpenOrdersFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.OpenOrders;

        public uint Limit;
    }
}
