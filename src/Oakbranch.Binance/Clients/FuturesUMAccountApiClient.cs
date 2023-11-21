using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Clients;

/// <summary>
/// Encapsulates functions for accessing the account data and trade endpoints of the Binance USD-Margined Futures API.
/// </summary>
public class FuturesUMAccountApiClient : FuturesUMClientBase
{
    #region Instance props & fields

    #endregion

    #region Instance constructors

    public FuturesUMAccountApiClient(
        IApiConnector connector,
        IRateLimitsRegistry limitsRegistry,
        ILogger<FuturesUMAccountApiClient>? logger = null)
        : base(connector, limitsRegistry, logger)
    { }

    #endregion

    #region Instance methods

    #endregion
}
