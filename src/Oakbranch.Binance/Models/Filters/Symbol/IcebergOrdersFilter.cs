using System;

namespace Oakbranch.Binance.Models.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the maximum number of iceberg orders an account is allowed to have open on a symbol.
    /// <para>An iceberg order is any order where <see cref="Order.IcebergQuantity"/> is greater than 0.</para>
    /// </summary>
    public sealed record IcebergOrdersFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.IcebergOrders;

        public uint Limit;
    }
}
