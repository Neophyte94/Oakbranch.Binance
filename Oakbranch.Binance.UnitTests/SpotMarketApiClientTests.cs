using System;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.RateLimits;
using Oakbranch.Binance.Spot;

namespace Oakbranch.Binance.UnitTests
{
    [TestFixture(true), Timeout(DefaultTestTimeout)]
    public class SpotMarketApiClientTests : ApiClientTestsBase
    {
        #region Constants

        private const int GlobalSetUpTimeout = 10000; // in ms.
        private const int GlobalSetUpRetryLimit = 3;
        public const int DefaultTestTimeout = 10000; // in ms.
        public const int DefaultTestRetryLimit = 2;

        #endregion

        #region Instance members

        private readonly SpotMarketApiClient m_Client;
        private readonly List<IDisposable> m_CleanupTargets;

        protected override string LogContext => "Spot Market Api Tester";

        #endregion

        #region Instance constructors

        public SpotMarketApiClientTests(bool areResultsLogged) : base(new ConsoleLogger(), areResultsLogged)
        {
            if (!ApiConnectorSource.TryReadApiKeysFromContainer(out string apiKey, out string? secretKey))
            {
                throw new Exception("The API connector cannot be created because API keys were not resolved.");
            }

            using IApiConnectorFactory apiConnFactory = ApiConnectorSource.CreateBuiltIn(apiKey, secretKey);
            IApiConnector connector = apiConnFactory.Create();
            IRateLimitsRegistry limitsRegistry = new RateLimitsRegistry();

            m_Client = new SpotMarketApiClient(connector, limitsRegistry, Logger);

            m_CleanupTargets = new List<IDisposable>();
            if (connector is IDisposable dsp2)
            {
                m_CleanupTargets.Add(dsp2);
            }
            if (limitsRegistry is IDisposable dsp3)
            {
                m_CleanupTargets.Add(dsp3);
            }
        }

        #endregion

        #region Instance methods

        // Initialization and finalization methods.
        [OneTimeSetUp]
        public async Task SetUpGlobalAsync()
        {
            int attemptsMade = 0;
            while (true)
            {
                CancellationTokenSource cts = new CancellationTokenSource(GlobalSetUpTimeout);
                try
                {
                    await m_Client.InitializeAsync(cts.Token).ConfigureAwait(false);
                    break;
                }
                catch (Exception exc)
                {
                    if (++attemptsMade >= GlobalSetUpRetryLimit)
                    {
                        throw;
                    }

                    LogMessage(LogLevel.Error, $"Initializing the API client failed:\r\n{exc}");
                }
            }
        }

        [OneTimeTearDown]
        public void TearDownGlobal()
        {
            m_Client.Dispose();
            foreach (IDisposable dsp in m_CleanupTargets)
            {
                dsp.Dispose();
            }
        }

        // Test methods.
        [Test, Retry(DefaultTestRetryLimit)]
        public async Task CheckServerTime_ReturnsValidTime_WhenExecuted()
        {
            // Act.
            DateTime result;
            using IDeferredQuery<DateTime> query = m_Client.PrepareCheckServerTime();
            result = await query.ExecuteAsync(CancellationToken.None);

            // Assert.
            Assert.That(result, Is.EqualTo(DateTime.UtcNow).Within(new TimeSpan(TimeSpan.TicksPerDay)));
        }

        #endregion
    }
}
