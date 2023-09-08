using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Represents an abstraction for a deferred web query.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by a web query.</typeparam>
    public interface IDeferredQuery<T> : IDisposable
    {
        /// <summary>
        /// Gets weight of a web query for rate limits.
        /// <para>
        /// The weights represent the query's footprint on specific limit weight dimensions.
        /// </para>
        /// </summary>
        IReadOnlyList<QueryWeight> Weights { get; }
        /// <summary>
        /// Executes a web query asynchronously.
        /// </summary>
        /// <param name="ct">A cancellation token for the operation.</param>
        /// <returns>The result of the query.</returns>
        Task<T> ExecuteAsync(CancellationToken ct);
    }
}