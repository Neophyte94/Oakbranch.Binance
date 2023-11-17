using System;

namespace Oakbranch.Binance.Models.Savings
{
    public readonly struct AggregateSavingsPosition
    {
        public readonly string Asset;
        public readonly decimal Amount;
        public readonly double AmountInBTC;
        public readonly double AmountInUSDT;

        public AggregateSavingsPosition(string asset, decimal amount, double amountInBtc, double amountInUsdt)
        {
            Asset = asset;
            Amount = amount;
            AmountInBTC = amountInBtc;
            AmountInUSDT = amountInUsdt;
        }
    }
}
