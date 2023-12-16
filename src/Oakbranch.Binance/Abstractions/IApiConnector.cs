using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Exceptions;

namespace Oakbranch.Binance.Abstractions;

/// <summary>
/// Represents functionality for signing web queries, sending them to the Binance API, and handling the protocol-level errors.
/// </summary>
public interface IApiConnector
{
    /// <summary>
    /// Gets the time provider used for timestamps.
    /// </summary>
    ITimeProvider TimeProvider { get; }

    /// <summary>
    /// Sends a web query with the specified query parameters asynchronously and returns the response.
    /// </summary>
    /// <param name="query">The query parameters to send.</param>
    /// <param name="ct">A cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns a response.</returns>
    /// <exception cref="ArgumentException">Thrown when the query parameters are undefined.</exception>
    /// <exception cref="QueryNotSupportedException">Thrown when the requested query requires signing, and the connector does not support it.</exception>
    /// <exception cref="QueryException">Thrown when the requested query cannot be executed for some reason.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before the operation completes.</exception>
    Task<Response> SendAsync(QueryParams query, CancellationToken ct);
    /// <summary>
    /// Determines whether a rate limit metrics map has been registered for the specified endpoint.
    /// </summary>
    /// <param name="relativeEndpoint">A relative endpoint to check a map for.</param>
    /// <returns><see langword="true"/> if a limit map is registered for the specified endpoint, otherwise <see langword="false"/>.</returns>
    bool IsLimitMetricsMapRegistered(string relativeEndpoint);
    /// <summary>
    /// Registers or updates a rate limit metrics map for the specified relative endpoint.
    /// <para>The limit metrics map is a collection of names of HTTP response headers to extract limits usage info from.</para>
    /// </summary>
    /// <param name="relativeEndpoint">The relative endpoint for which to register the limit metrics map.</param>
    /// <param name="limitKeysMap">The limit metrics map to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="limitKeysMap"/> is <see langword="null"/>.</exception>
    void SetLimitMetricsMap(string relativeEndpoint, IEnumerable<string> limitKeysMap);
}