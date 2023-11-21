using System;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Core.TimeProviders;

/// <summary>
/// Provides functionality for retrieving the current system time.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    #region Instance props & fields

    public DateTime UtcNow => DateTime.UtcNow;

    #endregion
}
