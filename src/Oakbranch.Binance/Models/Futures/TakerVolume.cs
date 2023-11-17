using System;

namespace Oakbranch.Binance.Models.Futures
{
    /// <summary>
    /// Represents information on volume of "taker" trades executed within a specific time period.
    /// </summary>
    public readonly struct TakerVolume
    {
        /// <summary>
        /// Defines the information timestamp.
        /// </summary>
        public readonly DateTime Timestamp;
        /// <summary>
        /// Defines the buy/sell volume ratio of "taker" trades executed.
        /// <para>The value is <see cref="double.NaN"/> if the data is not available.</para>
        /// </summary>
        public readonly double BuySellRatio;
        /// <summary>
        /// Defines the total buy volume of "taker" trades executed, in contracts number.</para>
        /// </summary>
        public readonly double BuyVolume;
        /// <summary>
        /// Defines the total sell volume of "taker" trades executed, in contracts number.
        /// </summary>
        public readonly double SellVolume;
        /// <summary>
        /// Defines the total buy value of "taker" trades executed, in base asset.
        /// <para>The value is <see cref="double.NaN"/> if the data is not available.</para>
        /// </summary>
        public readonly double TotalBuyValue;
        /// <summary>
        /// Defines the total sell value of "taker" trades executed, in base asset.
        /// <para>The value is <see cref="double.NaN"/> if the data is not available.</para>
        /// </summary>
        public readonly double TotalSellValue;

        /// <summary>
        /// Creates a new instance of the <see cref="TakerVolume"/> struct.
        /// </summary>
        /// <param name="timestamp">The information timestamp.</param>
        /// <param name="buySellRatio">The ratio between volume of buy and sell "taker" trades.
        /// <para>Use the value <see cref="double.NaN"/> if the data is not available.</para></param>
        /// <param name="buyVolume">The total buy volume of "taker" trades executed, in contracts number.</param>
        /// <param name="sellVolume">The total sell volume of "taker" trades executed, in contracts number.</param>
        /// <param name="totalBuyValue">The total buy value of "taker" trades executed, in base asset.
        /// <para>Use the value <see cref="double.NaN"/> if the data is not available.</para></param>
        /// <param name="totalSellValue">The total sell value of "taker" trades executed, in base asset.
        /// <para>Use the value <see cref="double.NaN"/> if the data is not available.</para></param>
        public TakerVolume(DateTime timestamp, double buySellRatio, double buyVolume, double sellVolume,
            double totalBuyValue, double totalSellValue)
        {
            Timestamp = timestamp;
            BuySellRatio = buySellRatio;
            BuyVolume = buyVolume;
            SellVolume = sellVolume;
            TotalBuyValue = totalBuyValue;
            TotalSellValue = totalSellValue;
        }
    }
}
