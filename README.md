# Oakbranch.Binance

Oakbranch.Binance is a .NET library that provides high-level access to the Binance REST API. It simplifies the process of retrieving market data, monitoring Binance accounts, and executing trades on the Binance platform.

## Features

- High-level access to Binance REST API endpoints through C# methods.
- Unified authentication entry point in the form of the "IApiConnector" abstraction and its built-in implementation.
- Separate web client classes for different API sections (Spot, Margin, Futures), each containing a set of related methods.
- Smart parameter validation in each wrapping method.
- Smart built-in error handling that classifies various low-level exceptions and error responses into instances of the unified high-level class "QueryException", which in turn has a distributed hierarchy of specialized exception classes.
- Deferred web query system can alternatively be used for precise rate limit management.
- Automatic updates of the rate limits registry through the "IRateLimitsRegistry" abstraction, which also has a built-in implementation.

## Getting Started

### Prerequisites

- .NET 7.0 or later
