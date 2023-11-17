using System;

namespace Oakbranch.Binance.Models.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the maximum number of "algo" orders an account is allowed to have open on a symbol. 
    /// <para>"Algo" orders are all variations of stop-loss and take-profit orders.</para>
    /// </summary>
    public sealed record AlgoOrdersFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.AlgoOrders;

        public uint Limit;
    }
}
