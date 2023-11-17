using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Models.Savings
{
    public sealed record SavingsAccountInfo
    {
        public List<AggregateSavingsPosition>? Positions;
        public double TotalAmountInBTC;
        public double TotalAmountInUSDT;
        public double TotalFixedAmountInBTC;
        public double TotalFixedAmountInUSDT;
        public double TotalFlexibleAmountInBTC;
        public double TotalFlexibleAmountInUSDT;
    }
}