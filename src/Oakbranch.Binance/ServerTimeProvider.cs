using System;
using System.Diagnostics;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Provides functionality for tracking and retrieving the estimated server time.
    /// <para>The time estimation is based on the specified server time zone and last known server time.</para>
    /// </summary>
    public class ServerTimeProvider : ITimeProvider
    {
        #region Instance members

        private readonly Stopwatch m_TimeCounter;
        private readonly long m_ServerZoneOffset;
        private long m_BaseTime;

        /// <summary>
        /// Gets the estimated server time.
        /// </summary>
        public DateTime EstimatedServerTime
        {
            get
            {
                return new DateTime(m_BaseTime + m_ServerZoneOffset + m_TimeCounter.Elapsed.Ticks);
            }
        }

        /// <summary>
        /// Gets the estimated current UTC time.
        /// </summary>
        public DateTime UtcNow
        {
            get
            {
                return new DateTime(m_BaseTime + m_TimeCounter.Elapsed.Ticks);
            }
        }

        #endregion

        #region Instance constructors

        /// <summary>
        /// Creates a new instance of <see cref="ServerTimeProvider"/> with the specified parameters.
        /// </summary>
        /// <param name="serverTimeZone">The time zone of the server.</param>
        /// <param name="serverNow">The last known server time.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serverTimeZone"/> is null.</exception>
        public ServerTimeProvider(TimeZoneInfo serverTimeZone, DateTime serverNow)
        {
            if (serverTimeZone == null)
                throw new ArgumentNullException(nameof(serverTimeZone));

            m_TimeCounter = new Stopwatch();
            m_ServerZoneOffset = serverTimeZone.BaseUtcOffset.Ticks;
            SetServerNow(serverNow);
        }

        #endregion

        #region Instance methods

        /// <summary>
        /// Restarts the time tracking with the specified server time.
        /// </summary>
        /// <param name="serverNow">The last known server time.</param>
        public void SetServerNow(DateTime serverNow)
        {
            m_BaseTime = serverNow.Ticks - m_ServerZoneOffset;
            m_TimeCounter.Restart();
        }

        #endregion
    }
}
