using System;

namespace Oakbranch.Binance.Filters.Symbol
{
    /// <summary>
    /// A filter that defines the maximum parts an iceberg order can have.
    /// <para>The number of iceberg parts is defined as a ceiling of (quantity / icebergQuantity).</para>
    /// </summary>
    public sealed record IcebergPartsFilter : SymbolFilter
    {
        public override SymbolFilterType Type => SymbolFilterType.IcebergParts;

        public uint Limit;
    }
}
