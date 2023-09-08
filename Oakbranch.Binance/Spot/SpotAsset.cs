using System;

namespace Oakbranch.Binance.Spot
{
    /// <summary>
    /// Represents state of a user's asset in the spot account.
    /// </summary>
    public readonly struct SpotAsset
    {
        /// <summary>
        /// Defines the asset's notation (e.g., BTC).
        /// </summary>
        public readonly string Asset;
        /// <summary>
        /// Defines the free quantity of the asset.
        /// </summary>
        public readonly decimal Free;
        /// <summary>
        /// Defines the locked quantity of the asset (in open orders, etc).
        /// </summary>
        public readonly decimal Locked;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpotAsset"/> struct.
        /// </summary>
        /// <param name="asset">The asset's notation (e.g., BTC).</param>
        /// <param name="free">The free quantity of the asset.</param>
        /// <param name="locked">The locked quantity of the asset.</param>
        public SpotAsset(string asset, decimal free, decimal locked)
        {
            Asset = asset;
            Free = free;
            Locked = locked;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="SpotAsset"/> struct.
        /// </summary>
        /// <returns>A string representation of the <see cref="SpotAsset"/> struct.</returns>
        public override string ToString()
        {
            return $"Spot asset {Asset}: Free = {Free}, Locked = {Locked}";
        }
    }
}
