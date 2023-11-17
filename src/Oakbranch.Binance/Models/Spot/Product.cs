using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Models.Spot
{
    /// <summary>
    /// Represents info on a trading pair.
    /// </summary>
    public struct Product
    {
        /// <summary>
        /// Gets an undefined instance of <see cref="Product"/>.
        /// </summary>
        public static Product Undefined
        {
            get
            {
                return new Product()
                {
                    Open = double.NaN,
                    High = double.NaN,
                    Low = double.NaN,
                    Close = double.NaN,
                    BaseVolume = double.NaN,
                    QuoteVolume = double.NaN,
                    CirculatingSupply = -1,
                    Tags = new List<string>(4),
                };
            }
        }

        /// <summary>
        /// Defines the symbol used for the trading pair.
        /// </summary>
        public string Symbol;
        /// <summary>
        /// Defines the current status of the trading pair.
        /// </summary>
        public string Status;
        /// <summary>
        /// Defines the symbol used for the base asset.
        /// </summary>
        public string BaseAsset;
        /// <summary>
        /// Defines the full name of the base asset.
        /// </summary>
        public string BaseAssetName;
        /// <summary>
        /// Defines the symbol used for the quote asset.
        /// </summary>
        public string QuoteAsset;
        /// <summary>
        /// Defines the full name of the quote asset.
        /// </summary>
        public string QuoteAssetName;
        /// <summary>
        /// Defines the price 24 hours ago.
        /// <para>If it's undefined the value is <see cref="double.NaN"/>.</para>
        /// </summary>
        public double Open;
        /// <summary>
        /// Defines the highest price within last 24 hours.
        /// <para>If it's undefined the value is <see cref="double.NaN"/>.</para>
        /// </summary>
        public double High;
        /// <summary>
        /// Defines the lowest price within last 24 hours.
        /// <para>If it's undefined the value is <see cref="double.NaN"/>.</para>
        /// </summary>
        public double Low;
        /// <summary>
        /// Defines the last price.
        /// <para>If it's undefined the value is <see cref="double.NaN"/>.</para>
        /// </summary>
        public double Close;
        /// <summary>
        /// Defines the trading volume of a pair in the base asset for last 24 hours.
        /// <para>If it's undefined the value is <see cref="double.NaN"/>.</para>
        /// </summary>
        public double BaseVolume;
        /// <summary>
        /// Defines the trading volume of a pair in the quote asset for last 24 hours.
        /// <para>If it's undefined the value is <see cref="double.NaN"/>.</para>
        /// </summary>
        public double QuoteVolume;
        /// <summary>
        /// Defines the circulating supply of the base asset.
        /// </summary>
        public long? CirculatingSupply;
        /// <summary>
        /// Defines a list of tags for the trading pair.
        /// </summary>
        public List<string> Tags;
    }
}
