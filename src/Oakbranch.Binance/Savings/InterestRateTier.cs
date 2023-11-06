using System;

namespace Oakbranch.Binance.Savings
{
    public struct InterestRateTier
    {
        public string Description;
        public double ExtraRate;

        public InterestRateTier(string description, double extraRate)
        {
            Description = description;
            ExtraRate = extraRate;
        }
    }
}
