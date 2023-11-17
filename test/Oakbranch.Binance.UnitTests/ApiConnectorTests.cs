
using Oakbranch.Binance.Abstractions;
using Oakbranch.Binance.Clients;
using Oakbranch.Binance.Core;

namespace Oakbranch.Binance.UnitTests
{
    [TestFixtureSource(typeof(ApiConnectorSource), nameof(ApiConnectorSource.CreateAllWithKeyContainer))]
    public class ApiConnectorTests
    {
        #region Static members

        private const int QueryShortTimeout = 5000; // in ms.
        private const int QueryLongTimeout = 15000; // in ms.

        #endregion

        #region Instance members

        private readonly string _baseEndpoint;
        private readonly string _relativeEndpoint;
        private readonly IApiConnectorFactory _connectorFactory;
        private IApiConnector? _connector;
         
        #endregion

        #region Instance constructors

        public ApiConnectorTests(IApiConnectorFactory connector)
        {
            _connectorFactory = connector ?? throw new ArgumentNullException(nameof(connector));
            _baseEndpoint = ApiV3ClientBase.RESTBaseEndpoints
                .First((e) => e.Type == NetworkType.Test)
                .Url;
            _relativeEndpoint = "/api/v3/time";
        }

        #endregion

        #region Instance methods

        // Setup and teardown.
        [SetUp]
        public void SetUpLocal()
        {
            _connector = _connectorFactory.Create();
        }

        [TearDown]
        public void TearDownLocal()
        {
            if (_connector is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _connector = null;
        }

        [OneTimeTearDown]
        public void TearDownGlobal()
        {
            _connectorFactory.Dispose();
        }

        // Unit tests.
        [Test]
        public void IsLimitMetricsMapRegistered_ReturnsTrue_WhenPassedRegistered()
        {
            // Arrange.
            _connector!.SetLimitMetricsMap(_baseEndpoint, new string[] { "X_MBX_TEST1" });

            // Act.
            bool result = _connector!.IsLimitMetricsMapRegistered(_baseEndpoint);

            // Assert.
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsLimitMetricsMapRegistered_ReturnsTrue_WhenPassedUppercaseRegistered()
        {
            // Arrange.
            _connector!.SetLimitMetricsMap(_baseEndpoint, new string[] { "X_MBX_TEST1" });

            // Act.
            bool result = _connector!.IsLimitMetricsMapRegistered(_baseEndpoint.ToUpperInvariant());

            // Assert.
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsLimitMetricsMapRegistered_ReturnsFalse_WhenPassedUnregistered()
        {
            // Arrange.
            // Not setting anything.

            // Act.
            bool result = _connector!.IsLimitMetricsMapRegistered(_baseEndpoint);

            // Assert.
            Assert.That(result, Is.False);
        }

        [Test, Timeout(QueryShortTimeout)]
        public async Task SendAsync_ThrowsArgumentException_WhenQueryUndefined()
        {
            // Arrange.
            QueryParams qp = default;

            // Act.
            Exception? result = null;
            try { await _connector!.SendAsync(qp, CancellationToken.None); }
            catch (Exception exc) { result = exc; }

            // Assert.
            Assert.That(result, Is.AssignableFrom<ArgumentException>());
        }

        [Test, Timeout(QueryShortTimeout)]
        public async Task SendAsync_DoesNotThrowArgumentException_WhenQuerySpecified()
        {
            // Arrange.
            QueryParams qp = CreateQueryParams();

            // Act.
            Exception? result = null;
            try { await _connector!.SendAsync(qp, CancellationToken.None); }
            catch (Exception exc) { result = exc; }

            // Assert.
            Assert.That(result, Is.Not.AssignableFrom<ArgumentException>());
        }

        [Test, Timeout(QueryShortTimeout), Retry(2)]
        public async Task SendAsync_ReturnsSuccessfulResponse_WhenQueryValid()
        {
            // Arrage
            QueryParams qp = CreateQueryParams();

            // Act.
            Response? result = await _connector!.SendAsync(qp, CancellationToken.None);

            // Assert.
            Assert.That(result?.IsSuccessful, Is.Not.Null.And.True);
        }

        [Test, Timeout(QueryShortTimeout), Retry(2)]
        public async Task SendAsync_ReturnsNotSuccessfulResponse_WhenQueryCorrupted()
        {
            // Arrage
            QueryParams qp = CreateQueryParams();
            qp = new QueryParams(HttpMethod.PUT, qp.BaseEndpoint, qp.RelativeEndpoint, null, false);

            // Act.
            Response? result = await _connector!.SendAsync(qp, CancellationToken.None);

            // Assert.
            Assert.That(result?.IsSuccessful, Is.Not.Null.And.False);
        }

        // Miscellaneous.
        private QueryParams CreateQueryParams(bool isSecured = false)
        {
            return new QueryParams(HttpMethod.GET, _baseEndpoint, _relativeEndpoint, null, isSecured);
        }

        #endregion
    }
}
