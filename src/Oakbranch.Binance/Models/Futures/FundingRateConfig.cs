using System;

namespace Oakbranch.Binance.Models.Futures;

/// <summary>
/// Represents information on a configuration of futures funding.
/// </summary>
public readonly struct FundingRateConfig
{
    /// <summary>
    /// Defines the futures contract symbol that the funding occurred for.
    /// </summary>
    public readonly string Symbol;

    /// <summary>
    /// Defines the upper limit for the adjusted funding rate.
    /// </summary>
    public readonly decimal Cap;

    /// <summary>
    /// Defines the lower limit for the adjusted funding rate.
    /// </summary>
    public readonly decimal Floor;

    /// <summary>
    /// Defines the time interval at which funding occurs, in hours.
    /// </summary>
    public readonly int FundingInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundingRateConfig"/> struct.
    /// </summary>
    /// <param name="symbol">The futures contract symbol.</param>
    /// <param name="cap">The upper limit for the adjusted funding rate.</param>
    /// <param name="floor">The lower limit for the adjusted funding rate.</param>
    /// <param name="fundingInterval">The time interval at which funding occurs (in hours).</param>
    public FundingRateConfig(string symbol, decimal cap, decimal floor, int fundingInterval)
    {
        Symbol = symbol;
        Cap = cap;
        Floor = floor;
        FundingInterval = fundingInterval;
    }
}