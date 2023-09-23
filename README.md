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
- [Oakbranch.Common](https://github.com/Neophyte94/Oakbranch.Common): An open-source .NET library used as a dependency in this project.

### Usage

To get started with making calls to the Binance API, follow these steps:

1. **Time Provider**: You must have an instance of `ITimeProvider`, which is used as a source of query timestamps. You can use one of the built-in implementations: `ServerTimeProvider` or `SystemTimeProvider`.

2. **Rate Limits Registry**: To track the current usage of API rate limits, you need an instance of `IRateLimitsRegistry`. Each high-level client class is responsible for adding relevant rate limits upon its initialization.

3. **API Connector**: You'll need an instance of `IApiConnector`, which serves as the unified entry point for low-level functions. It handles signing and sending web requests, processing responses, and handling errors. Consider using the built-in implementation `ApiConnector`.

4. **High-Level Client**: Create an instance of one of the high-level web client classes like `SpotMarketApiClient` or `SpotAccountApiClient`, depending on the specific API section you want to use. Then initialize the instance.

Once your client is set up, you can:
- Create and execute deferred queries.
- Or make direct queries using the client's methods.

```csharp
using System;
using System.Threading.Tasks;
using Oakbranch.Common.Logging;
using Oakbranch.Binance.RateLimits;
using Oakbranch.Binance.Spot;

public async Task TestDemoQueryAsync(string apiKey, string secretKey = null, CancellationToken ct = default)
{
    // For testing SystemTimeProvider is sufficient, but for production ServerTimeProvider is recommended.
    ITimeProvider timeProvider = new SystemTimeProvider();
    ILogger logger = new ConsoleLogger();
    IRateLimitsRegistry rateLimits = new RateLimitsRegistry();

    // Prepare variables for disposable objects.
    IApiConnector connector = null;
    SpotMarketApiClient client = null;
    try
    {
        // Initialize a low-level HTTP connector with API keys granted by Binance.
        connector = new ApiConnector(
            apiKey: apiKey,
            secretKey: secretKey,
            timeProvider: timeProvider,
            logger: logger);

        // Initialize a high-level client for accessing Spot Market endpoints.
        client = new SpotMarketApiClient(
            connector: connector,
            limitsRegistry: rateLimits,
            logger: logger);
        await client.InitializeAsync(default).ConfigureAwait(false);

        // Prepare a test web query for a deferred or immediate execution.
        DateTime serverTime;
        using (IDeferredQuery<DateTime> query = client.PrepareCheckServerTime())
        {
            // Call the query's execution whenever we are ready.
            serverTime = await query.ExecuteAsync(default).ConfigureAwait(false);
        }

        // Use the result of the query.
        Console.WriteLine($"The reported server time is {serverTime} (UTC).");
    }
    finally
    {
        client?.Dispose();
        if (connector is IDisposable disposableConnector)
        {
            disposableConnector.Dispose();
        }
    }
}
```

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE.txt](LICENSE.txt) file for details.
