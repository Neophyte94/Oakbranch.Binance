using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Oakbranch.Binance
{
    internal delegate Task<T> ExecuteQueryHandler<T>(
        QueryParams queryParams, IReadOnlyList<QueryWeight> weights,
        ParseResponseHandler<T> parseFunction, object parseArgs,
        IReadOnlyDictionary<string, int> headersToLimitsMap, CancellationToken ct);
}