using System;

namespace Oakbranch.Binance.Filters.Exchange
{
    /// <summary>
    /// A filter that defines the maximum number of "algo" orders an account is allowed to have open on the exchange. 
    /// <para>"Algo" orders are STOP_LOSS, STOP_LOSS_LIMIT, TAKE_PROFIT, and TAKE_PROFIT_LIMIT orders.</para>
    /// </summary>
    public sealed class TotalAlgoOrdersFilter : ExchangeFilter
    {
        public override ExchangeFilterType Type => ExchangeFilterType.TotalAlgoOrders;

        public uint Limit;
    }
}
