using System;

namespace Oakbranch.Binance.Models.Futures
{
    /// <summary>
    /// Represents an exchange-related information on a futures asset.
    /// </summary>
    public readonly struct AssetInfo
    {
        /// <summary>
        /// Defines the asset's notation (e.g., BTC).
        /// </summary>
        public readonly string Asset;
        /// <summary>
        /// Defines whether the asset can be used as a margin in the Multi-Asset mode.
        /// </summary>
        public readonly bool IsMarginAvailable;
        /// <summary>
        /// Defines the auto-exchange threshold in the Multi-Asset mode.
        /// <para>The value is <c>Null</c> for assets that cannot be used as a margin.</para>
        /// </summary>
        public readonly double? AutoExchangeThreshold;

        /// <summary>
        /// Creates a new instance of the <see cref="AssetInfo"/> struct.
        /// </summary>
        /// <param name="asset">The asset's notation (e.g., BTC).</param>
        /// <param name="isMarginAvailable">The asset can be used as a margin in the Multi-Asset mode.</param>
        /// <param name="autoExchangeThreshold">The auto-exchange threshold in the Multi-Asset mode.</param>
        public AssetInfo(string asset, bool isMarginAvailable, double? autoExchangeThreshold)
        {
            Asset = asset;
            IsMarginAvailable = isMarginAvailable;
            AutoExchangeThreshold = autoExchangeThreshold;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"Futures asset {Asset}: Is margin = {IsMarginAvailable}, Auto exchange threshold = {AutoExchangeThreshold}";
        }
    }
}
