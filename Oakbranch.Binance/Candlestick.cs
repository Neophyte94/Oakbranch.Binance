using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Represents a single candlestick on a financial chart.
    /// </summary>
    public readonly struct Candlestick
    {
        /// <summary>
        /// Defines the opening time of a candlestick.
        /// </summary>
        public readonly DateTime OpenTime;
        /// <summary>
        /// Defines the closing time of a candlestick.
        /// </summary>
        public readonly DateTime CloseTime;
        /// <summary>
        /// Defines the opening price of a candlestick.
        /// </summary>
        public readonly decimal Open;
        /// <summary>
        /// Defines the highest price reached within a candlestick's timeframe.
        /// </summary>
        public readonly decimal High;
        /// <summary>
        /// Defines the lowest price reached within a candlestick's timeframe.
        /// </summary>
        public readonly decimal Low;
        /// <summary>
        /// Defines the closing price of a candlestick.
        /// </summary>
        public readonly decimal Close;
        /// <summary>
        /// Defines a total base asset volume of all trades executed within a candlestick's timeframe.
        /// </summary>
        public readonly decimal BaseVolume;
        /// <summary>
        /// Defines a total quote asset volume of all trades executed within a candlestick's timeframe.
        /// </summary>
        public readonly decimal QuoteVolume;
        /// <summary>
        /// Defines a total number of trades executed within a candlestick's timeframe.
        /// </summary>
        public readonly uint NumberOfTrades;
        /// <summary>
        /// Defines a base asset volume of "taker" trades executed within a candlestick's timeframe.
        /// </summary>
        public readonly decimal TakerBaseVolume;
        /// <summary>
        /// Defines a quote asset volume of "taker" trades within a candlestick's timeframe.
        /// </summary>
        public readonly decimal TakerQuoteVolume;

        /// <summary>
        /// Initializes a new instance of the <see cref="Candlestick"/> struct with the full set of the properties.
        /// </summary>
        /// <param name="openTime">The opening time of the candlestick.</param>
        /// <param name="closeTime">The closing time of the candlestick.</param>
        /// <param name="open">The opening price of the candlestick.</param>
        /// <param name="high">The highest price reached within the candlestick's timeframe.</param>
        /// <param name="low">The lowest price reached within the candlestick's timeframe.</param>
        /// <param name="close">The closing price of the candlestick.</param>
        /// <param name="baseVolume">The total volume of all trades executed within the candlestick's timeframe, in the base asset.</param>
        /// <param name="quoteVolume">The total volume of all trades executed within the candlestick's timeframe, in the quote asset.</param>
        /// <param name="numberOfTrades">The total number of trades executed within the candlestick's timeframe.</param>
        /// <param name="takerBaseVolume">The volume of "taker" trades executed within the candlestick's timeframe, in the base asset.</param>
        /// <param name="takerQuoteVolume">The volume of "taker" trades executed within the candlestick's timeframe, in the quote asset.</param>
        public Candlestick(DateTime openTime, DateTime closeTime, decimal open, decimal high, decimal low, decimal close,
            decimal baseVolume, decimal quoteVolume, uint numberOfTrades, decimal takerBaseVolume, decimal takerQuoteVolume)
        {
            OpenTime = openTime;
            CloseTime = closeTime;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            BaseVolume = baseVolume;
            QuoteVolume = quoteVolume;
            NumberOfTrades = numberOfTrades;
            TakerBaseVolume = takerBaseVolume;
            TakerQuoteVolume = takerQuoteVolume;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Candlestick"/> struct with the partial set of the properties.
        /// </summary>
        /// <param name="openTime">The opening time of the candlestick.</param>
        /// <param name="closeTime">The closing time of the candlestick.</param>
        /// <param name="open">The opening price of the candlestick.</param>
        /// <param name="high">The highest price reached within the candlestick's timeframe.</param>
        /// <param name="low">The lowest price reached within the candlestick's timeframe.</param>
        /// <param name="close">The closing price of the candlestick.</param>
        public Candlestick(DateTime openTime, DateTime closeTime, decimal open, decimal high, decimal low, decimal close)
        {
            OpenTime = openTime;
            CloseTime = closeTime;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            BaseVolume = 0.0m;
            QuoteVolume = 0.0m;
            NumberOfTrades = 0;
            TakerBaseVolume = 0.0m;
            TakerQuoteVolume = 0.0m;
        }

        public override string ToString()
        {
            return $"Candlestick {OpenTime} - {CloseTime}: O={Open}, H={High}, L={Low}, C={Close}, V={BaseVolume}";
        }
    }
}
