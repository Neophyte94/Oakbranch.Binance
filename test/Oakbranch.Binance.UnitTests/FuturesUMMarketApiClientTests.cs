﻿using System;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Abstractions;
using Oakbranch.Binance.Clients;
using Oakbranch.Binance.Core.RateLimits;
using Oakbranch.Binance.Models;
using Oakbranch.Binance.Models.Futures;

namespace Oakbranch.Binance.UnitTests;

[TestFixture(true), Timeout(DefaultTestTimeout)]
public class FuturesUMMarketApiClientTests : ApiClientTestsBase
{
    #region Constants

    private const string DefaultSymbol = "BTCUSDT";

    #endregion

    #region Static members

    public static object[] CorrectQueryPeriodCases { get; } = new object[]
    {
        new object?[] { ReferenceDateTime.AddHours(-1.0), null },
        new object?[] { ReferenceDateTime.AddDays(-1.0).AddHours(-4.0), ReferenceDateTime.AddDays(-1.0) },
        new object?[] { null, ReferenceDateTime.AddDays(-1.0) },
        new object?[] { ReferenceDateTime.AddYears(-1), null },
        new object?[] { ReferenceDateTime.AddHours(-1.0).AddSeconds(-1.0), ReferenceDateTime.AddHours(-1.0) }
    };
    public static object[] InvalidQueryPeriodCases { get; } = new object[]
    {
        new object?[] { ReferenceDateTime.AddHours(-1.0), ReferenceDateTime.AddHours(-2.0), },
        new object?[] { ReferenceDateTime, ReferenceDateTime.AddSeconds(-1.0), },
    };
    public static object[] AllKlineIntervalCases { get; } = Enum.GetValues<KlineInterval>()
        .Select((i) =>
        {
            (TimeSpan min, TimeSpan max) = TestHelper.ParseInterval(i.ToString());
            return new object[] { i, min, max };
        })
        .ToArray();

    #endregion

    #region Instance props & fields

    private readonly FuturesUMMarketApiClient _client;
    private readonly List<IDisposable> _cleanupTargets;

    #endregion

    #region Instance constructors

    public FuturesUMMarketApiClientTests(bool areResultsLogged)
        : base(CreateDefaultLogger<FuturesUMMarketApiClientTests>(LogLevel.Information), areResultsLogged)
    {
        if (!ApiConnectorSource.TryReadApiKeysFromContainer(out string apiKey, out string? secretKey))
        {
            throw new Exception("The API connector cannot be created because API keys were not resolved.");
        }

        using IApiConnectorFactory apiConnFactory = ApiConnectorSource.CreateBuiltIn(apiKey, secretKey);
        IApiConnector connector = apiConnFactory.Create();
        IRateLimitsRegistry limitsRegistry = new RateLimitsRegistry();

        _client = new FuturesUMMarketApiClient(
            connector,
            limitsRegistry,
            CreateDefaultLogger<FuturesUMMarketApiClient>(LogLevel.Information));

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

    // Check server time.
    [Test, Retry(DefaultTestRetryLimit)]
    public async Task CheckServerTime_ReturnsValidTime_WhenDefaultParams()
    {
        // Arrange.
        DateTime result;

        // Act.
        using IDeferredQuery<DateTime> query = _client.PrepareCheckServerTime();
        result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert.
        Assert.That(result, Is.EqualTo(DateTime.UtcNow).Within(UtcTimeErrorTolerance));

        if (AreQueryResultsLogged)
        {
            LogMessage(LogLevel.Information, $"The server time reported: {result}.");
        }
    }

    // Get exchange info.
    [Test, Retry(DefaultTestRetryLimit)]
    public async Task GetExchangeInfo_ReturnsValidInstance_WhenDefaultParams()
    {
        // Arrange.
        FuturesExchangeInfo result;

        // Act.
        using IDeferredQuery<FuturesExchangeInfo> query = _client.PrepareGetExchangeInfo();
        result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert.
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Symbols, Is.Not.Null.And.Count.GreaterThan(0));
            Assert.That(result.Assets, Is.Not.Null.And.Count.GreaterThan(0));
            Assert.That(result.Timezone, Is.Not.Null);
        });

        if (AreQueryResultsLogged)
        {
            LogObject(result);
            LogCollection(result.Symbols);
            LogCollection(result.Assets);
        }
    }

    // Get old trades.
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
        Assert.That(result, Has.Count.EqualTo(FuturesUMMarketApiClient.DefaultTradesQueryLimit));

        if (AreQueryResultsLogged)
        {
            LogCollection(result, 10);
        }
    }

    [Retry(DefaultTestRetryLimit)]
    [TestCase(1)]
    [TestCase(FuturesUMMarketApiClient.MaxTradesQueryLimit - 1)]
    [TestCase(FuturesUMMarketApiClient.MaxTradesQueryLimit)]
    public async Task GetOldTrades_ReturnsExactCount_WhenSymbolAndLimitSpecified(int limit)
    {
        // Arrange.
        List<Trade> result;

        // Act.
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

    [TestCaseSource(nameof(NullAndWhitespaceStringCases))]
    public void GetOldTrades_ThrowsArgumentNullException_WhenEmptySymbolSpecified(string symbol)
    {
        // Arrange.
        TestDelegate td = new TestDelegate(() => _client.PrepareGetOldTrades(symbol));

        // Act & Assert.
        Assert.That(td, Throws.ArgumentNullException);
    }

    // Get aggregate trades.
    [TestCaseSource(nameof(CorrectQueryPeriodCases)), Retry(DefaultTestRetryLimit)]
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
            Assert.That(result.Select((at) => at.Timestamp), Has.All.GreaterThanOrEqualTo(from.Value));
        }
        if (to != null)
        {
            Assert.That(result.Select((at) => at.Timestamp), Has.All.LessThanOrEqualTo(to.Value));
        }

        if (AreQueryResultsLogged)
        {
            LogCollection(result, 10);
        }
    }

    [TestCaseSource(nameof(NullAndWhitespaceStringCases))]
    public void GetAggregateTrades_ThrowsArgumentNullException_WhenEmptySymbolSpecified(string symbol)
    {
        // Arrange.
        TestDelegate td = new TestDelegate(() => _client.PrepareGetAggregateTrades(symbol));

        // Act & Assert.
        Assert.That(td, Throws.ArgumentNullException);
    }

    [TestCaseSource(nameof(InvalidQueryPeriodCases))]
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
    [TestCase(FuturesUMMarketApiClient.MaxTradesQueryLimit + 1)]
    public void GetAggregateTrades_ThrowsArgumentOutOfRangeException_WhenInvalidLimitSpecified(int limit)
    {
        // Arrange.
        TestDelegate td = new TestDelegate(() =>
            _client.PrepareGetAggregateTrades(
                symbol: DefaultSymbol,
                limit: limit));

        // Act & Assert.
        Assert.That(td, Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    // Get symbol price klines.
    [Retry(DefaultTestRetryLimit)]
    [TestCase(1)]
    [TestCase(FuturesUMMarketApiClient.MaxKlinesQueryLimit)]
    [TestCase(FuturesUMMarketApiClient.MaxKlinesQueryLimit / 2 + 1)]
    public async Task GetSymbolPriceKlines_ReturnsExactCount_WhenLimitSpecified(int limit)
    {
        // Arrange.
        List<Candlestick> result;

        // Act.
        using IDeferredQuery<List<Candlestick>> query = _client.PrepareGetSymbolPriceKlines(
            symbol: DefaultSymbol,
            interval: KlineInterval.Hour1,
            limit: limit);
        result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert.
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(limit));

        if (AreQueryResultsLogged)
        {
            LogCollection(result, 10);
        }
    }

    [TestCaseSource(nameof(CorrectQueryPeriodCases)), Retry(DefaultTestRetryLimit)]
    public async Task GetSymbolPriceKlines_ReturnsItemsWithinPeriod_WhenPeriodSpecified(DateTime? from, DateTime? to)
    {
        // Arrange.
        List<Candlestick> result;

        // Act.
        using IDeferredQuery<List<Candlestick>> query = _client.PrepareGetSymbolPriceKlines(
            symbol: DefaultSymbol,
            interval: KlineInterval.Hour1,
            startTime: from,
            endTime: to,
            limit: null);
        result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert.
        Assert.That(result, Is.Not.Null);
        if (from != null)
        {
            Assert.That(result.Select((c) => c.OpenTime), Has.All.GreaterThanOrEqualTo(from.Value));
        }
        if (to != null)
        {
            Assert.That(result.Select((c) => c.OpenTime), Has.All.LessThanOrEqualTo(to.Value));
        }

        if (AreQueryResultsLogged)
        {
            LogCollection(result, 10);
        }
    }

    [TestCaseSource(nameof(AllKlineIntervalCases)), Retry(DefaultTestRetryLimit)]
    public async Task GetSymbolPriceKlines_ReturnsItemsOfExactTimeframe_WhenRequested(
        KlineInterval interval, TimeSpan minSpan, TimeSpan maxSpan)
    {
        // Arrange.
        List<Candlestick> result;

        // Act.
        using IDeferredQuery<List<Candlestick>> query = _client.PrepareGetSymbolPriceKlines(
            symbol: DefaultSymbol,
            interval: interval);
        result = await query.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert.
        Assert.That(result, Is.Not.Null);
        Assert.That(
            result.Select((c) => c.CloseTime - c.OpenTime),
            Has.All.InRange(minSpan, maxSpan));

        if (AreQueryResultsLogged)
        {
            LogCollection(result, 10);
        }
    }

    [TestCaseSource(nameof(NullAndWhitespaceStringCases))]
    public void GetSymbolPriceKlines_ThrowsArgumentNullException_WhenEmptySymbolSpecified(string symbol)
    {
        // Arrange.
        TestDelegate td = new TestDelegate(() => _client.PrepareGetSymbolPriceKlines(symbol, KlineInterval.Hour1));

        // Act & Assert.
        Assert.That(td, Throws.ArgumentNullException);
    }

    [TestCaseSource(nameof(InvalidQueryPeriodCases))]
    public void GetSymbolPriceKlines_ThrowsArgumentException_WhenInvalidPeriodSpecified(DateTime from, DateTime to)
    {
        // Arrange.
        TestDelegate td = new TestDelegate(() =>
            _client.PrepareGetSymbolPriceKlines(
                symbol: DefaultSymbol,
                interval: KlineInterval.Hour1,
                startTime: from,
                endTime: to));

        // Act & Assert.
        Assert.That(td, Throws.ArgumentException);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(FuturesUMMarketApiClient.MaxKlinesQueryLimit + 1)]
    public void GetSymbolPriceKlines_ThrowsArgumentOutOfRangeException_WhenInvalidLimitSpecified(int limit)
    {
        // Arrange.
        TestDelegate td = new TestDelegate(() =>
            {
                _client.PrepareGetSymbolPriceKlines(DefaultSymbol, KlineInterval.Hour1, limit: limit);
            });

        // Act & Assert.
        Assert.That(td, Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    #endregion
}