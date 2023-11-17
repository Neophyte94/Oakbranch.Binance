using System;
using NUnit.Framework.Internal;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.Models;
using Oakbranch.Binance.Models.Spot;
using Oakbranch.Binance.Clients;
using Oakbranch.Binance.Core.RateLimits;
using Oakbranch.Binance.Abstractions;

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

        private static readonly DateTime ReferenceDateTime = new DateTime(2023, 11, 01);
        private static readonly TimeSpan TimeErrorTolerance = new TimeSpan(TimeSpan.TicksPerHour);

        public static object[] CorrectAggrTradePeriodCases
        {
            get
            {
                return new object[]
                {
                    new object?[] { ReferenceDateTime.AddHours(-1.0), null },
                    new object?[] { ReferenceDateTime.AddDays(-1.0).AddHours(-1.0), ReferenceDateTime.AddDays(-1.0) },
                    new object?[] { null, ReferenceDateTime.AddDays(-1.0) },
                    new object?[] { ReferenceDateTime.AddYears(-1), null },
                    new object?[] { ReferenceDateTime.AddHours(-1.0).AddSeconds(-1.0), ReferenceDateTime.AddHours(-1.0) }
                };
            }
        }
        public static object[] InvalidAggrTradePeriodCases
        {
            get
            {
                return new object[]
                {
                    new object?[] { ReferenceDateTime, ReferenceDateTime.AddMilliseconds(-1.0), },
                    new object?[] { ReferenceDateTime.AddHours(-1.0), ReferenceDateTime.AddHours(-2.0), },
                };
            }
        }

        #endregion

        #region Instance members

        private readonly SpotMarketApiClient _client;
        private readonly List<IDisposable> _cleanupTargets;

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

            _client = new SpotMarketApiClient(connector, limitsRegistry, Logger);

            _cleanupTargets = new List<IDisposable>();
            if (connector is IDisposable dsp2)
            {
                _cleanupTargets.Add(dsp2);
            }
            if (limitsRegistry is IDisposable dsp3)
            {
                _cleanupTargets.Add(dsp3);
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
                    await _client.InitializeAsync(cts.Token).ConfigureAwait(false);
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
            _client.Dispose();
            foreach (IDisposable dsp in _cleanupTargets)
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
            using IDeferredQuery<DateTime> query = _client.PrepareCheckServerTime();
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
            using IDeferredQuery<SpotExchangeInfo> query = _client.PrepareGetExchangeInfo();
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Symbols, Is.Not.Null.And.Count.GreaterThan(0));
                Assert.That(result.ServerTime, Is.EqualTo(DateTime.UtcNow).Within(TimeErrorTolerance));
                Assert.That(result.Timezone, Is.Not.Null);
            });

            if (AreQueryResultsLogged)
            {
                LogObject(result);
            }
        }

        [Retry(DefaultTestRetryLimit)]
        [TestCase("BTCUSDT")]
        [TestCase("btcusdt", "eThUsDt")]
        public async Task GetExchangeInfo_ReturnsExactNumberOfSymbols_WhenExactSymbolsSpecified(params string[] symbols)
        {
            // Arrange.
            SpotExchangeInfo result;

            // Act.
            using IDeferredQuery<SpotExchangeInfo> query = _client.PrepareGetExchangeInfo(symbols);
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Symbols, Is.Not.Null.And.Count.EqualTo(symbols.Length));
                Assert.That(result.ServerTime, Is.EqualTo(DateTime.UtcNow).Within(TimeErrorTolerance));
                Assert.That(result.Timezone, Is.Not.Null);
            });

            if (AreQueryResultsLogged)
            {
                LogObject(result);
            }
        }

        // Get old trades tests.
        [Test, Retry(DefaultTestRetryLimit)]
        public async Task GetOldTrades_ReturnsDefaultCount_WhenOnlySymbolSpecified()
        {
            // Arrange.
            List<Trade> result;

            // Act;
            using IDeferredQuery<List<Trade>> query = _client.PrepareGetOldTrades(DefaultSymbol);
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(SpotMarketApiClient.DefaultTradesQueryLimit));

            if (AreQueryResultsLogged)
            {
                LogCollection(result, 10);
            }
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
            using IDeferredQuery<List<Trade>> query = _client.PrepareGetOldTrades(DefaultSymbol, limit: limit);
            result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert.
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(limit));

            if (AreQueryResultsLogged)
            {
                LogCollection(result, 10);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void GetOldTrades_ThrowsArgumentNullException_WhenEmptySymbolSpecified(string symbol)
        {
            // Arrange.
            TestDelegate td = new TestDelegate(() => _client.PrepareGetOldTrades(symbol));

            // Act & Assert.
            Assert.That(td, Throws.ArgumentNullException);
        }

        // Get aggregate trades tests.
        [TestCaseSource(nameof(CorrectAggrTradePeriodCases)), Retry(DefaultTestRetryLimit)]
        public async Task GetAggregateTrades_ReturnsItemsWithinPeriod_WhenPeriodSpecified(DateTime? from, DateTime? to)
        {
            // Arrange.
            List<AggregateTrade> result;

            // Act.
            using IDeferredQuery<List<AggregateTrade>> query = _client.PrepareGetAggregateTrades(
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

            if (AreQueryResultsLogged)
            {
                LogCollection(result, 10);
            }
        }

        [TestCaseSource(nameof(InvalidAggrTradePeriodCases))]
        public void GetAggregateTrades_ThrowsArgumentException_WhenInvalidPeriodSpecified(DateTime from, DateTime to)
        {
            // Arrange.
            TestDelegate td = new TestDelegate(() =>
                _client.PrepareGetAggregateTrades(
                    symbol: DefaultSymbol,
                    startTime: from,
                    endTime: to));

            // Act & Assert.
            Assert.That(td, Throws.ArgumentException);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(SpotMarketApiClient.MaxTradesQueryLimit + 1)]
        public void GetAggregateTrades_ThrowsArgumentOutOfRangeException_WhenInvalidLimitSpecified(int limit)
        {
            // Arrange.
            TestDelegate td = new TestDelegate(() =>
                _client.PrepareGetAggregateTrades(
                    symbol: DefaultSymbol,
                    limit: limit));

            // Act & Assert.
            Assert.That(td, Throws.Exception.AssignableFrom<ArgumentOutOfRangeException>());
        }

        #endregion
    }
}
