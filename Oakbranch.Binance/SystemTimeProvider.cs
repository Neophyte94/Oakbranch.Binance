using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Provides functionality for retrieving the current system time.
    /// </summary>
    public class SystemTimeProvider : ITimeProvider
    {
        #region Instance members

        public DateTime UtcNow => DateTime.UtcNow;

        #endregion
    }
}
