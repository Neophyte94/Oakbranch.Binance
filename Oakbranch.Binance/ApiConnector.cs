using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.Exceptions;

namespace Oakbranch.Binance;

/// <summary>
/// Provides basic connection functions for Binance API, including authentication, errors handling and checking rate limits.
/// <para>An instance must be initialized via <see cref="InitializeAsync"/> before being used.</para>
/// </summary>
public sealed class ApiConnector : IApiConnector, IDisposable
{
    #region Constants

    private const string ApiKeyHeaderName = "X-MBX-APIKEY";
    private const string LogContextName = "Binance API connector";
    public const int ApiKeyLength = 64;
    public const int SecretKeyLength = 64;
    public const ushort RequestWindowDefault = 5000;
    public const ushort RequestWindowMaximum = 60000;
    public const int RequestTimeoutDefault = 10000;
    public const int RequestTimeoutMaximum = 30000;
    private const long BanPreventionWaitTimeDefault = 2 * TimeSpan.TicksPerMinute;

    #endregion

    #region Instance members

    private readonly HttpClient m_Client;
    private byte[]? m_SecretKey;
    private ILogger? m_Logger;

    // A collection of the pairs "relative endpoint - rate limit metrics map"
    private readonly Dictionary<string, string[]> m_LimitMetricsMapsDict;

    private ITimeProvider m_TimeProvider;
    /// <summary>
    /// Gets or sets the time provider used for timestamps.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="ArgumentNullException"/>
    public ITimeProvider TimeProvider
    {
        get
        {
            ThrowIfDisposed();
            return m_TimeProvider;
        }
        set
        {
            ThrowIfDisposed();
            if (value == null)
                throw new ArgumentNullException(nameof(TimeProvider));
            m_TimeProvider = value;
        }
    }

    private readonly bool m_AreSecuredQueriesSupported;
    /// <summary>
    /// Gets a value indicating whether the connector instance is able to send secured queries. 
    /// <para>It depends on whether the instance was constructed with a secret API key provided.</para>
    /// </summary>
    public bool AreSecuredQueriesSupported => m_AreSecuredQueriesSupported;

    private ushort m_RequestWindow = RequestWindowDefault;
    /// <summary>
    /// Gets or sets the period after a request creation that the request is valid within (in ms).
    /// <para>The server automatically rejects all requests received within a bigger interval than this.</para>
    /// </summary>
    /// <value>
    /// Duration of the period in milliseconds that a request is valid within after its creation.
    /// <para>The default value is <see cref="RequestWindowDefault"/> (5000).
    /// The maximum value is <see cref="RequestWindowMaximum"/> (60000).</para>
    /// </value>
    public ushort RequestWindow
    {
        get
        {
            return m_RequestWindow;
        }
        set
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(
                    $"A request window cannot be zero.");
            if (value > RequestWindowMaximum)
                throw new ArgumentOutOfRangeException(
                    $"The specified request window value ({value}) is greater than the permitted maximum ({RequestWindowMaximum}).");
            m_RequestWindow = value;
        }
    }

    private int m_RequestTimeout = RequestTimeoutDefault;
    /// <summary>
    /// Gets or sets waiting duration for a web request's completion (in ms).
    /// <para>Any query task will be terminated in <see cref="RequestTimeout"/> ms, throwing
    /// <see cref="QueryException"/> with <see cref="FailureReason.Timeout"/>.</para>
    /// </summary>
    /// <value>
    /// Duration of a timeout for web requests in milliseconds.
    /// <para>The minimum value is 0. The maximum value is <see cref="RequestTimeoutMaximum"/> (30000).</para>
    /// <para>The defalt value is <see cref="RequestTimeoutDefault"/> (10000).</para>
    /// </value>
    public int RequestTimeout
    {
        get
        {
            return m_RequestTimeout;
        }
        set
        {
            if (value < 0 || value > RequestTimeoutMaximum)
                throw new ArgumentOutOfRangeException(nameof(RequestTimeout));
            m_RequestTimeout = value;
        }
    }

    private readonly Stopwatch m_BanPreventionTimer;
    private long m_BanPreventionDelta;
    private bool IsBanPreventionActive
    {
        get
        {
            if (m_BanPreventionDelta == 0)
            {
                return false;
            }
            else
            {
                UpdateBanPreventionDelay();
                return m_BanPreventionDelta == 0;
            }
        }
    }
    /// <summary>
    /// Gets the time remained till the ban prevention delay expires.
    /// </summary>
    /// <value>
    /// The time remained till the delay expiry, or <see cref="TimeSpan.Zero"/> if there is no active delay at the moment.
    /// </value>
    public TimeSpan BanPreventionDelay
    {
        get
        {
            ThrowIfDisposed();
            UpdateBanPreventionDelay();
            
            if (m_BanPreventionTimer.IsRunning)
            {
                long ticksLeft = m_BanPreventionDelta - m_BanPreventionTimer.ElapsedTicks;
                return new TimeSpan(ticksLeft > 0 ? ticksLeft : 0);
            }
            else
            {
                return TimeSpan.Zero;
            }
        }
    }

    private bool m_IsDisposed;
    /// <summary>
    /// Gets a value indicating whether the <see cref="ApiConnector"/> instance has been disposed.
    /// </summary>
    public bool IsDisposed => m_IsDisposed;

    #endregion

    #region Instance constructors

    /// <summary>
    /// Creates a new instance of the <see cref="ApiConnector"/> class with the specified parameters for public queries only.
    /// <para>The resulting instance will not support queries marked as secured (<see cref="QueryParams.IsSecured"/>).</para>
    /// </summary>
    /// <param name="apiKey">
    /// The API key to use for authentication.
    /// <para>The value must have the length <see cref="ApiKeyLength"/> (32).</para>
    /// </param>
    /// <param name="timeProvider">The time provider to use for timestamps (optional).
    /// <para>If not specified, the <see cref="ApiConnector"/> instance will use the system time.</para>
    /// </param>
    /// <param name="logger">
    /// The logger to use for logging messages (optional).
    /// <para>If not specified, logging will be disabled.</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="apiKey"/> parameter is null or empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="apiKey"/> parameter has an invalid length.
    /// </exception>
    public ApiConnector(string apiKey, ITimeProvider? timeProvider = null, ILogger? logger = null) :
        this(apiKey, null, timeProvider, logger) { }

    /// <summary>
    /// Creates a new instance of the <see cref="ApiConnector"/> class with the specified parameters.
    /// </summary>
    /// <param name="apiKey">
    /// The API key to use for authentication.
    /// <para>The value must have the length <see cref="ApiKeyLength"/> (32).</para>
    /// </param>
    /// <param name="secretKey">
    /// The secret key to use for digital signing.
    /// <para>Use the <c>Null</c> value to restrict the <see cref="ApiConnector"/> instance to public queries only.</para>
    /// <para>The non-null value must have the length <see cref="SecretKeyLength"/> (64).</para>
    /// </param>
    /// <param name="timeProvider">The time provider to use for timestamps (optional).
    /// <para>If not specified, the <see cref="ApiConnector"/> instance will use the system time.</para>
    /// </param>
    /// <param name="logger">
    /// The logger to use for logging messages (optional).
    /// <para>If not specified, logging will be disabled.</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="apiKey"/> parameter is null or empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either the <paramref name="apiKey"/> or the <paramref name="secretKey"/> parameter has an invalid length.
    /// </exception>
    public ApiConnector(string apiKey, string? secretKey, ITimeProvider? timeProvider = null, ILogger? logger = null)
    {
        if (String.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        if (apiKey.Length != ApiKeyLength)
            throw new ArgumentException($"The specified API key is of the invalid length ({apiKey.Length}). " +
                $"The valid key is a {ApiKeyLength}-character string.");
        
        m_AreSecuredQueriesSupported = !String.IsNullOrEmpty(secretKey);
        if (m_AreSecuredQueriesSupported)
        {
            if (secretKey!.Length != SecretKeyLength)
                throw new ArgumentException("The specified secret key is of the invalid length " +
                    $"({secretKey.Length}). The valid key is a {SecretKeyLength}-character string.");
            m_SecretKey = Encoding.ASCII.GetBytes(secretKey);
        }

        m_Client = new HttpClient();
        m_Client.Timeout = new TimeSpan(TimeSpan.TicksPerMillisecond * RequestTimeoutMaximum + TimeSpan.TicksPerSecond);
        m_Client.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        m_TimeProvider = timeProvider ?? new SystemTimeProvider();
        m_Logger = logger;
        m_LimitMetricsMapsDict = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);
        m_BanPreventionTimer = new Stopwatch();
    }

    #endregion

    #region Static methods

    private static bool TryDecodeFailureReason(int statusCode, out FailureReason value)
    {
        switch (statusCode)
        {
            case 403:
                value = FailureReason.WAFLimitViolated;
                return true;
            case 418:
                value = FailureReason.IPAutoBanned;
                return true;
            case 429:
                value = FailureReason.RateLimitViolated;
                return true;
            default:
                value = default;
                return false;
        }
    }

    private static QueryException GenerateQueryException(int statusCode, string message)
    {
        if (TryDecodeFailureReason(statusCode, out FailureReason reason))
        {
            return new QueryException(reason, message);
        }
        else
        {
            return new QueryException(FailureReason.Other, $"{statusCode}: {message}.");
        }
    }

    private static QueryException GenerateQueryException(Exception exception)
    {
        return new QueryException(null, exception);
    }

    #endregion

    #region Instance methods

    // Query sending and response processing.
    /// <summary>
    /// Sends a query with the specified parameters asynchronously.
    /// <para>If <see cref="QueryParams.IsSecured"/> is <see langword="true"/> the query is automatically signed.</para>
    /// <para>It is recommended to reserve extra 121 character space in the <see cref="QueryParams.QueryString"/> instance
    /// for secured queries, provided the query string is not empty.</para>
    /// </summary>
    /// <param name="query">The parameters of the query to send.</param>
    /// <param name="ct">The cancellation token for the web operation.</param>
    /// <returns>A task responsible for sending the query and fetching </returns>
    /// <exception cref="QueryNotSupportedException">
    /// Thrown when the specified query is marked as secured while
    /// the <see cref="ApiConnector"/> instance was not provided with the secret API key.</exception>
    public async Task<Response> SendAsync(QueryParams query, CancellationToken ct)
    {
        ThrowIfDisposed();
        if (query.IsUndefined)
        {
            throw new ArgumentException("The specified query parameters are empty.");
        }
        if (query.IsSecured && !m_AreSecuredQueriesSupported)
        {
            throw new QueryNotSupportedException($"This {nameof(ApiConnector)} instance does not support secured queries.");
        }
        if (IsBanPreventionActive)
        {
            throw new QueryException(FailureReason.BanPreventionBlock);
        }

        string? fullEndpoint = null;
        CancellationTokenSource timeoutCts = new CancellationTokenSource(m_RequestTimeout);
        CancellationTokenSource unitedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
        try
        {
            QueryBuilder queryString = query.QueryString;
            if (query.IsSecured)
            {
                CompleteAndSign(ref queryString);
            }

            fullEndpoint = CreateFullEndpoint(query.BaseEndpoint, query.RelativeEndpoint, queryString);    

            if (IsLogLevelEnabled(LogLevel.Debug))
            {
                PostLogMessage(LogLevel.Debug, $"Sending the {query.Method} request: {fullEndpoint}");
            }

            Task<HttpResponseMessage> rspTask;
            switch (query.Method)
            {
                case HttpMethod.GET:
                    rspTask = m_Client.GetAsync(fullEndpoint, unitedCts.Token);
                    break;
                case HttpMethod.PUT:
                    rspTask = m_Client.PutAsync(fullEndpoint, null, unitedCts.Token);
                    break;
                case HttpMethod.POST:
                    rspTask = m_Client.PostAsync(fullEndpoint, null, unitedCts.Token);
                    break;
                case HttpMethod.DELETE:
                    rspTask = m_Client.DeleteAsync(fullEndpoint, unitedCts.Token);
                    break;
                default:
                    throw new NotImplementedException($"The http method \"{query.Method}\" is not supported.");
            }

            HttpResponseMessage response = await rspTask.ConfigureAwait(false);
            byte[] content;
            try
            {
                content = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                throw new QueryException("Content of the HTTP response cannot be read.", exc);
            }

            if (response.IsSuccessStatusCode)
            {
                List<KeyValuePair<string, string>>? limitsUsage = null;
                try
                {
                    limitsUsage = ExtractLimitMetrics(response.Headers, query.RelativeEndpoint);
                }
                catch (Exception exc)
                {
                    PostLogMessage(
                        LogLevel.Error,
                        $"Failed to extract the rate limits usage info from the HTTP headers:\r\n{exc}");
                }

                return new Response(content, limitsUsage ?? new List<KeyValuePair<string, string>>(0));
            }
            else
            {
                long? retryAfterDelay = DetermineRetryAfterTime(response.StatusCode, response.Headers);
                if (retryAfterDelay != null)
                {
                    ActivateBanPreventionDelay(retryAfterDelay.Value);
                }

                return new Response(content);
            }
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            PostLogMessage(LogLevel.Error, $"The web request \"{fullEndpoint}\" was cancelled by the caller.");
            throw new OperationCanceledException(ct);
        }
        catch (TaskCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            PostLogMessage(LogLevel.Error, $"The web request \"{fullEndpoint}\" timed out.");
            throw new QueryException(FailureReason.Timeout);
        }
        catch (HttpRequestException httpExc) when (httpExc.InnerException is WebException webExc)
        {
            PostLogMessage(LogLevel.Error, $"The web request \"{fullEndpoint}\" failed because the server couldn't be reached.");
            throw new QueryException(FailureReason.ConnectionFailed);
        }
        catch (WebException wExc)
        {
            PostLogMessage(LogLevel.Error, $"The web request \"{fullEndpoint}\" failed with the status \"{wExc.Status}\".");
            int statusCode = wExc.Response is HttpWebResponse webResponse ? (int)webResponse.StatusCode : 0;
            throw GenerateQueryException(statusCode, wExc.Message);
        }
        catch (Exception exc)
        {
            PostLogMessage(LogLevel.Error, $"The web request \"{fullEndpoint}\" failed:\r\n{exc}");
            throw GenerateQueryException(exc);
        }
        finally
        {
            unitedCts.Dispose();
            timeoutCts.Dispose();
        }
    }

    private string CreateFullEndpoint(string baseEndpoint, string relativeEndpoint, QueryBuilder queryString)
    {
        if (queryString != null)
        {
            return String.Format("{0}{1}?{2}", baseEndpoint, relativeEndpoint, queryString.ToQuery());
        }
        else
        {
            return String.Concat(baseEndpoint, relativeEndpoint);
        }
    }

    /// <summary>
    /// Adds security parameters to the specified query string (or creates the one with those parameters),
    /// signs the complete query and finally adds the signature to the query string.
    /// </summary>
    /// <param name="rawQuery">A query string to sign (without security parameters). Can be null.</param>
    private void CompleteAndSign(ref QueryBuilder rawQuery)
    {
        ThrowIfDisposed();

        rawQuery ??= new QueryBuilder(123);
        rawQuery.AddParameter("recvWindow", m_RequestWindow);
        rawQuery.AddParameter("timestamp", CommonUtility.ConvertToApiTime(m_TimeProvider.UtcNow));

        string signature;
        using (HMACSHA256 signProvider = new HMACSHA256(m_SecretKey!))
        {
            byte[] buffer = Encoding.ASCII.GetBytes(rawQuery.ToQuery());
            buffer = signProvider.ComputeHash(buffer);
            signature = BitConverter.ToString(buffer).Replace("-", "").ToLower();
        }

        rawQuery.AddParameter("signature", signature);
    }

    private List<KeyValuePair<string, string>>? ExtractLimitMetrics(HttpResponseHeaders headers, string relativeEndpoint)
    {
        // Find a limit metrics map registered for the hierarchically closest endpoint.
        // For example, provided the relativeEndpoint is "\api\v3\ping":
        // 1) the function will look for "\api\v3\ping" pair; if the map is registered for this endpoint - return it;
        // 2) otherwise the function will look for "\api\v3\ pair", end so on.
        string[]? limitMetricsMap = null;
        foreach (string url in CommonUtility.TraversePathHierarchy(relativeEndpoint))
        {
            if (m_LimitMetricsMapsDict.TryGetValue(url, out limitMetricsMap))
            {
                break;
            }
        }

        // Check whether the limit metrics map is found.
        if (limitMetricsMap == null)
        {
            if (m_LimitMetricsMapsDict.Count != 0)
            {
                PostLogMessage(
                    LogLevel.Debug,
                    "The limit metrics won't be returned because no limit metrics map " +
                    "has been registered in the API connector instance.");
            }
            else
            {
                PostLogMessage(
                    LogLevel.Debug,
                    $"None of {m_LimitMetricsMapsDict.Count} registered limit metrics maps " +
                    $"can be applied to the relative endpoint \"{relativeEndpoint}\".");
            }

            return null;
        }

        // Check whether the limit metrics is required for this endpoint.
        if (limitMetricsMap.Length == 0)
        {
            PostLogMessage(
                LogLevel.Debug,
                $"The limit metrics won't be returned because the limit metrics map " +
                $"applied to the endpoint \"{relativeEndpoint}\" is empty.");
            return null;
        }

        // Search HTTP headers for the required limit metrics.
        List<KeyValuePair<string, string>> extractedValues = new List<KeyValuePair<string, string>>(limitMetricsMap.Length);
        foreach (string key in limitMetricsMap)
        {
            if (headers.TryGetValues(key, out IEnumerable<string>? headerValues))
            {
                string? headerValue = null;
                foreach (string val in headerValues)
                {
                    if (!String.IsNullOrEmpty(val))
                    {
                        headerValue = val;
                        break;
                    }
                }

                if (headerValue != null)
                {
                    extractedValues.Add(new KeyValuePair<string, string>(key, headerValue));
                }
                else
                {
                    PostLogMessage(
                        LogLevel.Warning,
                        $"The HTTP header \"{key}\" associated with the limit metrics " +
                        $"for the endpoint \"{relativeEndpoint}\" contains no real values.");
                }
            }
        }

        if (extractedValues.Count == 0)
        {
            PostLogMessage(
                LogLevel.Warning, 
                $"The HTTP response headers contain none of {limitMetricsMap.Length} keys " +
                $"specified in the limit metrics map that is applied to the endpoint \"{relativeEndpoint}\".");
        }

        // Return the result.
        return extractedValues;
    }

    /// <summary>
    /// Determines the delay required before the next query according to the specified response info, and returns its duration in ticks.
    /// </summary>
    private long? DetermineRetryAfterTime(HttpStatusCode statusCode, HttpResponseHeaders headers)
    {
        RetryConditionHeaderValue? retryValue = headers.RetryAfter;

        if (retryValue != null)
        {
            if (retryValue.Delta != null)
            {
                return retryValue.Delta.Value.Ticks;
            }
            else
            {
                PostLogMessage(
                    LogLevel.Error,
                    $"The retry after time cannot be retrieved from HTTP response headers. " +
                    $"The default delay will be returned.");
                return BanPreventionWaitTimeDefault;
            }
        }
        else
        {
            return (int)statusCode switch
            {
                418 or 429 => BanPreventionWaitTimeDefault,
                _ => null,
            };
        }
    }

    // Rate limit metrics.
    /// <summary>
    /// Checks whether a headers' names map of rate limit metrics has been registered for the specified endpoint,
    /// and returns the result.
    /// </summary>
    /// <param name="relativeEndpoint">The discriminative endpoint to check the map status for.</param>
    /// <returns><see langword="true"/> if the map has been registered for <paramref name="relativeEndpoint"/>, otherwise <see langword="false"/>.</returns>
    public bool IsLimitMetricsMapRegistered(string relativeEndpoint)
    {
        return m_LimitMetricsMapsDict.ContainsKey(relativeEndpoint);
    }

    /// <summary>
    /// Registers the specified map, or replaces an existing one, for the specified endpoint.
    /// </summary>
    /// <param name = "relativeEndpoint" >
    /// The base endpoint to register or set the map for.
    /// <para>Any unregistered endpoints that start with the specified endpoint will also be associated with this map.</para>
    /// </param>
    /// <param name="limitKeysMap">
    /// The collection of names of HTTP response headers to get rate limit metrics from.
    /// <para>This map will be used for the specified endpoint and all unregistered sub-endpoints.</para>
    /// </param>
    public void SetLimitMetricsMap(string relativeEndpoint, IEnumerable<string> limitKeysMap)
    {
        if (limitKeysMap == null)
            throw new ArgumentNullException(nameof(limitKeysMap));
        lock (m_LimitMetricsMapsDict)
        {
            m_LimitMetricsMapsDict[relativeEndpoint] = limitKeysMap.ToArray();
        }
    }

    // Ban prevention.
    private void ActivateBanPreventionDelay(long delayInTicks)
    {
        m_BanPreventionDelta = delayInTicks;
        m_BanPreventionTimer.Restart();
    }

    private void UpdateBanPreventionDelay()
    {
        if (m_BanPreventionTimer.IsRunning)
        {
            if (m_BanPreventionTimer.ElapsedTicks > m_BanPreventionDelta)
            {
                CancelBanPreventionDelay();
            }
        }
    }

    /// <summary>
    /// Cancels the currently active ban prevention delay.
    /// <para>See <see cref="BanPreventionDelay"/> for details.</para>
    /// </summary>
    public void CancelBanPreventionDelay()
    {
        m_BanPreventionTimer.Stop();
        m_BanPreventionDelta = 0;
    }

    // Miscellaneous.
    private bool IsLogLevelEnabled(LogLevel level)
    {
        return m_Logger != null && m_Logger.IsLevelEnabled(level);
    }

    private void PostLogMessage(LogLevel level, string message)
    {
        m_Logger?.Log(level, LogContextName, message);
    }

    private void ThrowIfDisposed()
    {
        if (m_IsDisposed)
        {
            throw new ObjectDisposedException(nameof(ApiConnector));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool releaseManaged)
    {
        if (m_IsDisposed) return;
        m_IsDisposed = true;

        Common.Utility.CommonUtility.Clear(ref m_SecretKey);

        if (releaseManaged)
        {
            m_Client?.Dispose();
            m_TimeProvider = null!;
            m_Logger = null;
            m_LimitMetricsMapsDict.Clear();
            m_BanPreventionTimer.Stop();
        }
    }

    #endregion

    #region Destructor

    ~ApiConnector()
    {
        Dispose(false);
    }

    #endregion
}
