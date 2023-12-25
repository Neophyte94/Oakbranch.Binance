using System;

namespace Oakbranch.Binance.Models.Futures;

/// <summary>
/// Defines information about delivery of a futures contract.
/// </summary>
public readonly struct DeliveryInfo
{
    /// <summary>
    /// Defines the time when the cotract is settled.
    /// </summary>
    public readonly DateTime Time;

    /// <summary>
    /// Defines the settlement price of the contract.
    /// </summary>
    public readonly decimal Price;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryInfo"/> struct.
    /// </summary>
    /// <param name="time">The time when the contract is settled.</param>
    /// <param name="price">The settlement price of the contract.</param>
    public DeliveryInfo(DateTime time, decimal price)
    {
        Time = time;
        Price = price;
    }
}