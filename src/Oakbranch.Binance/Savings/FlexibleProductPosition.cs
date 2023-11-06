using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Savings
{
    /// <summary>
    /// Represents a flexible product position which allows daily rewards for assets deposited.
    /// </summary>
    public struct FlexibleProductPosition
    {
        /// <summary>
        /// Defines the asset that the flexible product is based on.
        /// </summary>
        public string Asset;
        /// <summary>
        /// Defines the unique identifier of the flexible product.
        /// </summary>
        public string ProductId;
        /// <summary>
        /// Defines the descriptive name of the flexible product.
        /// </summary>
        public string ProductName;
        /// <summary>
        /// Defines whether the funds subscribed for the product can be redeemed.
        /// </summary>
        public bool CanRedeem;
        /// <summary>
        /// Defines the total quantity of the asset located in the position.
        /// </summary>
        public decimal TotalAmount;
        /// <summary>
        /// The designation of the field is unknown.
        /// </summary>
        public decimal FreeAmount;
        /// <summary>
        /// Defines the quantity of the asset that is currently being redeemed from the position.
        /// </summary>
        public decimal RedeemingAmount;
        /// <summary>
        /// The designation of the field is unknown.
        /// </summary>
        public decimal CollateralAmount;
        /// <summary>
        /// Defines the amount of the asset added to the position today.
        /// </summary>
        public decimal TodayPurchasedAmount;
        /// <summary>
        /// Defines the cummulative quantity of all rewards earned within this position.
        /// </summary>
        public decimal TotalInterest;
        /// <summary>
        /// Defines the cummulative quantity of the rewards earned within higher interest rate tiers of this position.
        /// </summary>
        public decimal TotalBonusRewards;
        /// <summary>
        /// Defines the cummulative quantity of the rewards earned within the base interest rate tier of this position.
        /// </summary>
        public decimal TotalMarketRewards;
        /// <summary>
        /// Defines the daily interest rate.
        /// </summary>
        public decimal DailyInterestRate;
        /// <summary>
        /// Defines the annual interest rate.
        /// </summary>
        public decimal AnnualInterestRate;
        /// <summary>
        /// Defines the annual interest rates for different position size tiers.
        /// <para>The value is <c>Null</c> if the flexible product does not have interest rate tiers.</para>
        /// </summary>
        public List<InterestRateTier> AnnualInterestRateTiers;
    }
}
