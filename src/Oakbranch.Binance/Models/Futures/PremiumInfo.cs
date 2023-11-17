using System;

namespace Oakbranch.Binance.Models.Futures
{
    /// <summary>
    /// Represents information on the current premium for a futures contract.
    /// </summary>
    public readonly struct PremiumInfo
    {
        /// <summary>
        /// Defines the symbol of the futures contract.
        /// </summary>
        public readonly string Symbol;
        /// <summary>
        /// Defines the underlying trading pair of the futures contract.
        /// </summary>
        public readonly string Pair;
        /// <summary>
        /// Defines the mark price of the symbol.
        /// </summary>
        public readonly decimal MarkPrice;
        /// <summary>
        /// Defines the index price of the symbol.
        /// </summary>
        public readonly decimal IndexPrice;
        /// <summary>
        /// Defines the estimated settlement price of the symbol.
        /// <para>It is only useful in the last hour before the settlement starts.</para>
        /// </summary>
        public readonly decimal EstimatedSettlePrice;
        /// <summary>
        /// Defines the most recent unsettled funding rate for the symbol (for perpetual contracts only).
        /// <para>The value is <c>Null</c> for non-perpetual contracts.</para>
        /// </summary>
        public readonly decimal? LastFundingRate;
        /// <summary>
        /// Defines the base asset interest rate (for perpetual contracts only).
        /// <para>The value is <c>Null</c> for non-perpetual contracts.</para>
        /// </summary>
        public readonly decimal? InterestRate;
        /// <summary>
        /// Defines the date &amp; time of the next funding for the symbol (for perpetual contracts only).
        /// <para>The value is <c>Null</c> for non-perpetual contracts.</para>
        /// </summary>
        public readonly DateTime? NextFundingTime;
        /// <summary>
        /// Defines the information timestamp.
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Creates a new instance of the <see cref="PremiumInfo"/> struct for a perpetual contract info.
        /// </summary>
        /// <param name="symbol">The symbol of the futures contract.</param>
        /// <param name="pair">The underlying trading pair of the futures contract.</param>
        /// <param name="markPrice">The mark price of the symbol.</param>
        /// <param name="indexPrice">The index price of the symbol.</param>
        /// <param name="estimatedSettlePrice">The estimated settlement price of the symbol.</param>
        /// <param name="lastFundingRate">The last unsettled funding rate for the symbol.</param>
        /// <param name="interestRate">The base asset interest rate.</param>
        /// <param name="nextFundingTime">The time of the next funding for the symbol.</param>
        /// <param name="timestamp">The information timestamp.</param>
        public PremiumInfo(string symbol, string pair, decimal markPrice, decimal indexPrice, decimal estimatedSettlePrice,
            decimal? lastFundingRate, decimal? interestRate, DateTime? nextFundingTime, DateTime timestamp)
        {
            Symbol = symbol;
            Pair = pair;
            MarkPrice = markPrice;
            IndexPrice = indexPrice;
            EstimatedSettlePrice = estimatedSettlePrice;
            LastFundingRate = lastFundingRate;
            InterestRate = interestRate;
            NextFundingTime = nextFundingTime;
            Timestamp = timestamp;
        }
    }
}