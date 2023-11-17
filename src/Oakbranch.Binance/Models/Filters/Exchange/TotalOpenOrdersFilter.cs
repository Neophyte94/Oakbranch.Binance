using System;

namespace Oakbranch.Binance.Models.Filters.Exchange
{
    /// <summary>
    /// A filter that defines the maximum number of orders an account is allowed to have open on the exchange.
    /// <para>Note that both "algo" orders and normal orders are counted for this filter.</para>
    /// </summary>
    public sealed record TotalOpenOrdersFilter : ExchangeFilter
    {
        public override ExchangeFilterType Type => ExchangeFilterType.TotalOpenOrders;

        public uint Limit;
    }
}