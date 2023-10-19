using System;

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

        private readonly string m_BaseEndpoint;
        private readonly string m_RelativeEndpoint;
        private readonly IApiConnectorFactory m_ConnectorFactory;
        private IApiConnector? m_Connector;
         
        #endregion

        #region Instance constructors

        public ApiConnectorTests(IApiConnectorFactory connector)
        {
            m_ConnectorFactory = connector ?? throw new ArgumentNullException(nameof(connector));
            m_BaseEndpoint = ApiV3ClientBase.RESTBaseEndpoints
                .First((e) => e.Type == NetworkType.Test)
                .Url;
            m_RelativeEndpoint = "/api/v3/time";
        }

        #endregion

        #region Instance methods

        // Setup and teardown.
        [SetUp]
        public void SetUpLocal()
        {
            m_Connector = m_ConnectorFactory.Create();
        }

        [TearDown]
        public void TearDownLocal()
        {
            if (m_Connector is IDisposable disposable)
            {
                disposable.Dispose();
            }
            m_Connector = null;
        }

        [OneTimeTearDown]
        public void TearDownGlobal()
        {
            if (m_ConnectorFactory is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        // Unit tests.

        [Test]
        public void IsLimitMetricsMapRegistered_ReturnsTrue_WhenPassedRegistered()
        {
            // Arrange.
            m_Connector!.SetLimitMetricsMap(m_BaseEndpoint, new string[] { "X_MBX_TEST1" });

            // Act.
            bool result = m_Connector!.IsLimitMetricsMapRegistered(m_BaseEndpoint);

            // Assert.
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsLimitMetricsMapRegistered_ReturnsTrue_WhenPassedUppercaseRegistered()
        {
            // Arrange.
            m_Connector!.SetLimitMetricsMap(m_BaseEndpoint, new string[] { "X_MBX_TEST1" });

            // Act.
            bool result = m_Connector!.IsLimitMetricsMapRegistered(m_BaseEndpoint.ToUpperInvariant());

            // Assert.
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsLimitMetricsMapRegistered_ReturnsFalse_WhenPassedUnregistered()
        {
            // Arrange.
            // Not setting anything.

            // Act.
            bool result = m_Connector!.IsLimitMetricsMapRegistered(m_BaseEndpoint);

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
            try { await m_Connector!.SendAsync(qp, CancellationToken.None); }
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
            try { await m_Connector!.SendAsync(qp, CancellationToken.None); }
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
            Response? result = await m_Connector!.SendAsync(qp, CancellationToken.None);

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
            Response? result = await m_Connector!.SendAsync(qp, CancellationToken.None);

            // Assert.
            Assert.That(result?.IsSuccessful, Is.Not.Null.And.False);
        }

        // Miscellaneous.
        private QueryParams CreateQueryParams(bool isSecured = false)
        {
            return new QueryParams(HttpMethod.GET, m_BaseEndpoint, m_RelativeEndpoint, null, isSecured);
        }

        #endregion
    }
}
