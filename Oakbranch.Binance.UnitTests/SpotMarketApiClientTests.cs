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

        private const string DefaultSymbol = "BTCUSDT";
        private const int GlobalSetUpTimeout = 10000; // in ms.
        private const int GlobalSetUpRetryLimit = 3;
        public const int DefaultTestTimeout = 10000; // in ms.
        public const int DefaultTestRetryLimit = 2;

        #endregion

        #region Static members

        private static readonly TimeSpan TimeErrorTolerance = new TimeSpan(TimeSpan.TicksPerHour);

        private static object[] HistoricalPeriodCases
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                return new object[]
                {
                    new object?[] { now.AddDays(-1.0), null },
                    new object?[] { now.AddDays(-1.0).AddHours(-1.0), now.AddDays(-1.0) },
                    new object?[] { null, now.AddDays(-1.0) },
                    new object?[] { now.AddYears(-1), null },
                    new object?[] { now.AddDays(-1.0).AddSeconds(-1.0), now.AddDays(-1.0) }
                };
            }
        }

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

        // Initialization and finalization.
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

        // Check server time tests.
        [Test, Retry(DefaultTestRetryLimit)]
        public async Task CheckServerTime_ReturnsValidTime_WhenDefaultParams()
        {
            // Arrange.
            DateTime result;

            // Act.
            using IDeferredQuery<DateTime> query = m_Client.PrepareCheckServerTime();
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.EqualTo(DateTime.UtcNow).Within(TimeErrorTolerance));
        }

        // Get exchange info tests.
        [Test, Retry(DefaultTestRetryLimit)]
        public async Task GetExchangeInfo_ReturnsValidInstance_WhenDefaultParams()
        {
            // Arrange.
            SpotExchangeInfo result;

            // Act.
            using IDeferredQuery<SpotExchangeInfo> query = m_Client.PrepareGetExchangeInfo();
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Symbols, Is.Not.Null.And.Count.GreaterThan(0));
                Assert.That(result.ServerTime, Is.EqualTo(DateTime.UtcNow).Within(TimeErrorTolerance));
                Assert.That(result.Timezone, Is.Not.Null);
            });
            LogObject(result);
        }

        [Retry(DefaultTestRetryLimit)]
        [TestCase("BTCUSDT")]
        [TestCase("btcusdt", "eThUsDt")]
        public async Task GetExchangeInfo_ReturnsExactNumberOfSymbols_WhenExactSymbolsSpecified(params string[] symbols)
        {
            // Arrange.
            SpotExchangeInfo result;

            // Act.
            using IDeferredQuery<SpotExchangeInfo> query = m_Client.PrepareGetExchangeInfo(symbols);
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Symbols, Is.Not.Null.And.Count.EqualTo(symbols.Length));
                Assert.That(result.ServerTime, Is.EqualTo(DateTime.UtcNow).Within(TimeErrorTolerance));
                Assert.That(result.Timezone, Is.Not.Null);
            });
            LogObject(result);
        }

        // Get old trades tests.
        [Test, Retry(DefaultTestRetryLimit)]
        public async Task GetOldTrades_ReturnsDefaultCount_WhenOnlySymbolSpecified()
        {
            // Arrange.
            List<Trade> result;

            // Act;
            using IDeferredQuery<List<Trade>> query = m_Client.PrepareGetOldTrades(DefaultSymbol);
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(SpotMarketApiClient.DefaultTradesQueryLimit));
            LogCollection(result, 10);
        }

        [Retry(DefaultTestRetryLimit)]
        [TestCase(1)]
        [TestCase(SpotMarketApiClient.MaxTradesQueryLimit - 1)]
        [TestCase(SpotMarketApiClient.MaxTradesQueryLimit)]
        public async Task GetOldTrades_ReturnsExactCount_WhenSymbolAndLimitSpecified(int limit)
        {
            // Arrange.
            List<Trade> result;

            // Act;
            using IDeferredQuery<List<Trade>> query = m_Client.PrepareGetOldTrades(DefaultSymbol, limit: limit);
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(limit));
            LogCollection(result, 10);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void GetOldTrades_ThrowsArgumentNullException_WhenEmptySymbolSpecified(string symbol)
        {
            // Arrange.
            TestDelegate td = new TestDelegate(() => m_Client.PrepareGetOldTrades(symbol));

            // Act & Assert.
            Assert.That(td, Throws.ArgumentNullException);
        }

        // Get aggregate trades tests.
        [TestCaseSource(nameof(HistoricalPeriodCases)), Retry(DefaultTestRetryLimit)]
        public async Task GetAggregateTrades_ReturnsItemsWithinPeriod_WhenPeriodSpecified(DateTime? from, DateTime? to)
        {
            // Arrange.
            List<AggregateTrade> result;

            // Act.
            using IDeferredQuery<List<AggregateTrade>> query = m_Client.PrepareGetAggregateTrades(
                symbol: DefaultSymbol,
                startTime: from,
                endTime: to,
                limit: null);
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            if (from != null)
            {
                Assert.That(result, Has.All.Matches((AggregateTrade at) => at.Timestamp >= from.Value));
            }
            if (to != null)
            {
                Assert.That(result, Has.All.Matches((AggregateTrade at) => at.Timestamp <= to.Value));
            }
            LogCollection(result, 10);
        }

        #endregion
    }
}
