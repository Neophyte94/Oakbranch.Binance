using System;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Abstractions;
using Oakbranch.Binance.Clients;
using Oakbranch.Binance.Core.RateLimits;
using Oakbranch.Binance.Models.Futures;

namespace Oakbranch.Binance.UnitTests;

[TestFixture(true), Timeout(DefaultTestTimeout)]
public class FuturesUMMarketApiClientTests : ApiClientTestsBase
{
    #region Constants

    private const string DefaultSymbol = "BTCUSDT";

    #endregion

    #region Static members

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
        : base(CreateDefaultLogger<SpotMarketApiClientTests>(LogLevel.Information), areResultsLogged)
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
        Assert.That(result, Is.EqualTo(DateTime.UtcNow).Within(UtcTimeErrorTolerance));

        if (AreQueryResultsLogged)
        {
            LogMessage(LogLevel.Information, $"The server time reported: {result}.");
        }
    }

    // Get exchange info tests.
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
            Assert.That(result.ServerTime, Is.EqualTo(DateTime.UtcNow).Within(UtcTimeErrorTolerance));
            Assert.That(result.Timezone, Is.Not.Null);
        });

        if (AreQueryResultsLogged)
        {
            LogObject(result);
        }
    }

    #endregion
}