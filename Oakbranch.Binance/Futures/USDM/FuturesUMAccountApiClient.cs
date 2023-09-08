using System;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.RateLimits;

namespace Oakbranch.Binance.Futures.USDM
{
    /// <summary>
    /// Encapsulates functions for accessing the account data and trade endpoints of the Binance USD-Margined Futures API.
    /// </summary>
    public class FuturesUMAccountApiClient : FuturesUMClientBase
    {
        #region Instance members

        #endregion

        #region Instance constructors

        public FuturesUMAccountApiClient(IApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger logger = null) :
            base(connector, limitsRegistry, logger)
        { }

        #endregion

        #region Instance methods

        #endregion
    }
}
