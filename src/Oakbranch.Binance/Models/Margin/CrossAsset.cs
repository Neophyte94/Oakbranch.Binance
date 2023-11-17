using System;

namespace Oakbranch.Binance.Models.Margin
{
    /// <summary>
    /// Represents an asset in the cross margin account.
    /// </summary>
    public readonly struct CrossAsset
    {
        /// <summary>
        /// Defines the asset's notation (e.g., BTC).
        /// </summary>
        public readonly string Asset;
        /// <summary>
        /// Defines the borrowed amount of the asset.
        /// </summary>
        public readonly decimal Borrowed;
        /// <summary>
        /// Defines the free quantity asset.
        /// </summary>
        public readonly decimal Free;
        /// <summary>
        /// Defines the interest accrued for the asset.
        /// </summary>
        public readonly decimal Interest;
        /// <summary>
        /// Defines the quantity of the asset locked by the system (in orders etc).
        /// </summary>
        public readonly decimal Locked;
        /// <summary>
        /// Defines the net quantity of the asset.
        /// </summary>
        public readonly decimal NetAsset;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossAsset"/> struct.
        /// </summary>
        /// <param name="asset">The asset's notation (e.g., BTC).</param>
        /// <param name="borrowed">The amount of the asset borrowed.</param>
        /// <param name="free">The free amount of the asset.</param>
        /// <param name="interest">The interest accrued for the asset.</param>
        /// <param name="locked">The amount of the asset locked by the system.</param>
        /// <param name="netAsset">The net value of the asset.</param>
        public CrossAsset(string asset, decimal borrowed, decimal free, decimal interest, decimal locked, decimal netAsset)
        {
            Asset = asset;
            Borrowed = borrowed;
            Free = free;
            Interest = interest;
            Locked = locked;
            NetAsset = netAsset;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"Cross margin asset {Asset}: Free = {Free}, Borrowed = {Borrowed}, Interest = {Interest}, Locked = {Locked}";
        }
    }
}