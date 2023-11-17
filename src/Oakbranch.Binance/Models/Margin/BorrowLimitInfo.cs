using System;

namespace Oakbranch.Binance.Models.Margin
{
    /// <summary>
    /// Represents information on borrow limits of a margin account.
    /// </summary>
    public readonly struct BorrowLimitInfo
    {
        /// <summary>
        /// Defines the maximum borrowable quantity determined by the account's current state of assets.
        /// <para>The value depends on the current amount of collateral deposited, the current debt and the max margin level.</para>
        /// <para>The (in)sufficient system availability of the asset is not counted.</para>
        /// <para>For example, if a user operates the BTC/USDT isolated account with the maximum x5 margin level, 
        /// has 100 own USDT, 30 borrowed BUSD and 0.6 USDT interest charged, then the amount of borrowable USDT available
        /// for him is:<code>5.0 * 100.0 - 30.0 - 0.6 = 469.4</code></para>
        /// </summary>
        public readonly decimal StateLimit;
        /// <summary>
        /// Defines the maximum borrowable quantity limited by the account level.
        /// </summary>
        public readonly decimal LevelLimit;

        /// <summary>
        /// Creates an instance of the <see cref="BorrowLimitInfo"/> struct.
        /// </summary>
        /// <param name="stateLimit">The maximum borrowable quantity determined by the account's current state of assets.</param>
        /// <param name="levelLimit">The maximum borrowable quantity limited by the account level.</param>
        public BorrowLimitInfo(decimal stateLimit, decimal levelLimit)
        {
            StateLimit = stateLimit;
            LevelLimit = levelLimit;
        }
    }
}