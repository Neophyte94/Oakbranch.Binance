using System;

namespace Oakbranch.Binance.Abstractions
{
    /// <summary>
    /// Represents functionality for retrieving the current date &amp; time.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Gets the current UTC date &amp; time.
        /// </summary>
        DateTime UtcNow { get; }
    }
}