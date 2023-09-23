# Oakbranch.Binance

Oakbranch.Binance is a .NET library that provides high-level access to the Binance REST API. The library facilitates an object-oriented programming (OOP) approach for interacting with Binance servers, covering tasks like fetching market data, monitoring accounts, and executing trades.

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

- .NET 7.0 or later
