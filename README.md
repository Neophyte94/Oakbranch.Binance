# Oakbranch.Binance

Oakbranch.Binance is a .NET library that provides high-level access to the Binance REST API. The library facilitates an OOP approach for interacting with Binance servers, covering tasks like fetching market data, monitoring accounts, and executing trades.

## Features

- High-level access to Binance REST API via C# methods.
- Unified authentication entry point with the "IApiConnector" abstraction and its built-in implementation.
- Dedicated web client classes for different API sections (Spot, Margin, Futures, etc), each housing a set of related methods.
- Smart parameter validation in each endpoint-wrapping method.
- Robust built-in error handling categorizing low-level web exceptions and error JSON responses into specialized instances of the "QueryException" class.
- Deferred web query system is available for (optional) precise rate limit management.
- Automatic updates of rate limits usage through the "IRateLimitsRegistry" abstraction, which also has a built-in implementation.

## Getting Started

### Prerequisites

- .NET 7.0 or later.

### Usage

To get started with making calls to the Binance API, follow these steps:

1. **Time Provider**: You must have an instance of `ITimeProvider`, which is used as a source of query timestamps. You can use one of the built-in implementations: `ServerTimeProvider` or `SystemTimeProvider`.

2. **Rate Limits Registry**: To track the current usage of API rate limits, you need an instance of `IRateLimitsRegistry`. Each high-level client class is responsible for adding relevant rate limits upon its initialization.

3. **API Connector**: You'll need an instance of `IApiConnector`, which serves as the unified entry point for low-level functions. It handles signing and sending web requests, processing responses, and handling errors. Consider using the built-in implementation `ApiConnector`.

4. **High-Level Client**: Create an instance of one of the high-level web client classes like `SpotMarketApiClient` or `SpotAccountApiClient`, depending on the specific API section you want to use. Then initialize the instance.

Once your client is set up, you can:
- Create and execute deferred queries.
- Or make direct queries using the client's methods.

Below is an example that demonstrates the full initialization process and how to retrieve server time using a deferred query.

```csharp
public static async Task TestDemoQueryAsync(string apiKey, string secretKey = null, CancellationToken ct = default)
{
    // For testing 'SystemTimeProvider' is sufficient, but for production 'ServerTimeProvider' is recommended.
    ITimeProvider timeProvider = new SystemTimeProvider();
    IRateLimitsRegistry rateLimits = new RateLimitsRegistry();

    // Initialize a low-level HTTP connector with API keys granted by Binance.
    using ApiConnector connector = new ApiConnector(apiKey, secretKey, timeProvider);

    // Initialize a high-level web client for accessing Spot Market endpoints.
    using SpotMarketApiClient client = new SpotMarketApiClient(connector, rateLimits);
    await client.InitializeAsync(ct).ConfigureAwait(false);

    // Prepare a test web query for a deferred or immediate execution.
    DateTime serverTime;
    using IDeferredQuery<DateTime> query = client.PrepareCheckServerTime();

    // Call the query's execution whenever we are ready.
    serverTime = await query.ExecuteAsync(ct).ConfigureAwait(false);

    // Use the result of the query.
    Console.WriteLine($"The reported server time is {serverTime} (UTC).");
}
```

In each client class, web queries are encapsulated in methods that come in pairs:
- one for creating a deferred query object (e.g., `PrepareCheckServerTime()`);
- another for immediate query execution (e.g., `CheckServerTimeAsync()`).

The 'immediate' method serves as a shortcut for preparing and executing a query in a single line, but it limits the scheduling and prioritizing flexibility in multi-query scenarios. For instance, the server time query, demonstrated above with a two-step execution, may be accomplished in one line as follows:

```csharp
DateTime serverTime = await client.CheckServerTimeAsync(ct).ConfigureAwait(false);
```

While the abstraction `IApiConnector` allows any custom implementation of the web query signing, the built-in class `ApiConnector` relies on the provided implementation of `ITimeProvider` for signing queries with valid timestamps. A significant deviation from the server time may lead to requests being rejected by the server.

Therefore, it is strongly recommended to employ a precise implementation of `ITimeProvider`, such as the built-in `ServerTimeProvider` class. Below is a possible way to instantiate it using an already initialized web client. Note that for initialization purposes a simple implementation like `SystemTimeProvider` can be used in `ApiConnector`, and later replaced with a more accurate time provider.

```csharp
public static async Task<ITimeProvider> CreateTimeProviderAsync(SpotMarketApiClient client, CancellationToken ct)
{
    // Declare variables for the task.
    Stopwatch pingTimer = new Stopwatch();
    DateTime serverTime;

    // Create the server time query.
    using IDeferredQuery<DateTime> serverTimeQuery = client.PrepareCheckServerTime();

    // Ensure the sufficient clearance in the current rate limit usage.
    if (!client.LimitsRegistry.TestUsage(serverTimeQuery.Weights, out _))
    {
        // Wait till the limits are reset.
        // In production, a more advanced waiting approach is recommended.
        await Task.Delay(1000, ct).ConfigureAwait(false);
    }

    // Execute the query.
    pingTimer.Start();
    serverTime = await serverTimeQuery.ExecuteAsync(ct).ConfigureAwait(false);
    pingTimer.Stop();

    // Create the time provider and return it.
    ITimeProvider timeProvider = new ServerTimeProvider(
        serverTimeZone: TimeZoneInfo.Utc,
        serverNow: serverTime + new TimeSpan(pingTimer.Elapsed.Ticks / 2));
    return timeProvider;
}
```

The precise time provider can be assigned to the existing instance of the `ApiConnector` class using the public setter of the `TimeProvider` property. Note that the `IApiConnector` interface itself only provides a getter for this property, as the means by which a connector acquires its time provider falls outside the scope of the abstraction.

```csharp
ApiConnector connector = reference_to_existing_connector as ApiConnector;
if (connector != null)
{
    connector.TimeProvider = precise_time_provider;
}
```

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE.txt](LICENSE.txt) file for details.