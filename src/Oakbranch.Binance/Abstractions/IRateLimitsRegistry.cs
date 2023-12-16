using System;
using System.Collections.Generic;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Core.RateLimits;

namespace Oakbranch.Binance.Abstractions;

/// <summary>
/// Represents a registry for rate limits associated with different weight dimensions.
/// </summary>
public interface IRateLimitsRegistry
{
    /// <summary>
    /// Gets a snapshot of a rate limit with the specified ID.
    /// </summary>
    /// <param name="id">An identifier of a rate limit to get info for.</param>
    /// <returns>A snapshot of a rate limit information associated with the specified ID.</returns>
    RateLimitInfo this[int id] { get; }

    /// <summary>
    /// Tries to register a new rate limit with the specified parameters, and returns the operation's result.
    /// </summary>
    /// <param name="id">The ID of the new rate limit.</param>
    /// <param name="limitParams">The parameters of the new rate limit.</param>
    /// <returns><see langword="true"/> if the limit has been successfully registered, otherwise <see langword="false"/>.</returns>
    bool TryRegisterLimit(int id, RateLimitInfo limitParams);
    /// <summary>
    /// Sets the maximum usage level of the rate limit with the specified ID to the specified value.
    /// </summary>
    /// <param name="id">The ID of the rate limit to modify.</param>
    /// <param name="newLimit">The new value of the maximum usage level of the rate limit.</param>
    void ModifyLimit(int id, uint newLimit);
    /// <summary>
    /// Determines whether a rate limit with the specified ID is registered.
    /// </summary>
    /// <param name="id">The ID of the rate limit to check.</param>
    /// <returns><see langword="true"/> if a rate limit with the specified ID is registered, otherwise <see langword="false"/>.</returns>
    bool ContainsLimit(int id);
    /// <summary>
    /// Determines whether an action with the specified weights violates any registered rate limits.
    /// </summary>
    /// <param name="weights">The weights defining the additional footprint on each affected weight dimension.</param>
    /// <param name="violatedLimitId">An output parameter to receive the ID of the violated rate limit (if any).</param>
    /// <returns>
    /// <see langword="true"/> if none of the limits is potentially violated, otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when any of the weight dimensions specified in <paramref name="weights"/> is not found.
    /// </exception>
    bool TestUsage(IReadOnlyList<QueryWeight> weights, out int violatedLimitId);
    /// <summary>
    /// Increases the usage level of each rate limit associated with the weight dimensions specified in <paramref name="weights"/>.
    /// </summary>
    /// <param name="weights">The weights defining the additional footprint on each affected weight dimension.</param>
    /// <param name="timestamp">The timestamp of the operation responsible for the usage level incremental (in UTC).</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when any of the weight dimensions specified in <paramref name="weights"/> is not found.
    /// </exception>
    void IncrementUsage(IReadOnlyList<QueryWeight> weights, DateTime timestamp);
    /// <summary>
    /// Updates the current usage level of the specified rate limit, if the provided data is not older than the last update.
    /// </summary>
    /// <param name="id">The ID of the rate limit to update the usage for.</param>
    /// <param name="usage">The new value of the current usage level of the rate limit.</param>
    /// <param name="timestamp">The timestamp of the provided usage value (in UTC).
    /// <para>If the exact timestamp is unknown, a pessimistic estimation is preferred.</para></param>
    void UpdateUsage(int id, uint usage, DateTime timestamp);
}