using System;

namespace Oakbranch.Binance.Models.Futures;

/// <summary>
/// Represents information on a historical futures funding.
/// </summary>
public readonly struct FundingRate
{
    /// <summary>
    /// Defines the futures contract symbol that the funding occurred for.
    /// </summary>
    public readonly string Symbol;
    /// <summary>
    /// Defines the date &amp; time when the funding occurred.
    /// </summary>
    public readonly DateTime Time;
    /// <summary>
    /// Defines the funding rate.
    /// </summary>
    public readonly decimal Rate;
    /// <summary>
    /// Defines the mark price associated with this particular funding fee charge.
    /// </summary>
    public readonly decimal? MarkPrice;

    public FundingRate(string symbol, DateTime time, decimal rate, decimal? markPrice)
    {
        Symbol = symbol;
        Time = time;
        Rate = rate;
        MarkPrice = markPrice;
    }
}
