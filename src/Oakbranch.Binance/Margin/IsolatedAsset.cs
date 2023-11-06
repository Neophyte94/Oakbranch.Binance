using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Represents information on a certain asset of an isolated margin account.
    /// </summary>
    public readonly struct IsolatedAsset
    {
        /// <summary>
        /// Defines an asset's notation (e.g., BTC).
        /// </summary>
        public readonly string Asset;
        /// <summary>
        /// Defines whether borrowing is enabled for this asset.
        /// </summary>
        public readonly bool IsBorrowEnabled;
        /// <summary>
        /// Defines whether repaying is enabled for this asset.
        /// </summary>
        public readonly bool IsRepayEnabled;
        /// <summary>
        /// Defines the quantity of this asset that has been borrowed.
        /// </summary>
        public readonly decimal Borrowed;
        /// <summary>
        /// Defines the quantity of this asset that is free to use.
        /// </summary>
        public readonly decimal Free;
        /// <summary>
        /// Defines the amount of interest accrued on this asset.
        /// </summary>
        public readonly decimal Interest;
        /// <summary>
        /// Defines the quantity of this asset that is locked in an order.
        /// </summary>
        public readonly decimal Locked;
        /// <summary>
        /// Defines the net asset value of this asset.
        /// </summary>
        public readonly decimal NetAsset;
        /// <summary>
        /// Defines the net asset value of this asset, denominated in Bitcoin.
        /// </summary>
        public readonly decimal NetAssetOfBTC;
        /// <summary>
        /// Defines the total quantity of this asset.
        /// </summary>
        public readonly decimal TotalAsset;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolatedAsset"/> struct.
        /// </summary>
        /// <param name="asset">The asset's notation (e.g., BTC).</param>
        /// <param name="isBorrowEnabled">Whether borrowing is enabled for this asset.</param>
        /// <param name="isRepayEnabled">Whether repaying is enabled for this asset.</param>
        /// <param name="borrowed">The quantity of this asset that has been borrowed.</param>
        /// <param name="free">The quantity of this asset that is free to use.</param>
        /// <param name="interest">The amount of interest accrued on this asset.</param>
        /// <param name="locked">The quantity of this asset that is locked in an order.</param>
        /// <param name="netAsset">The net asset value of this asset.</param>
        /// <param name="netAssetOfBTC">The net asset value of this asset, denominated in Bitcoin.</param>
        /// <param name="totalAsset">The total quantity of this asset.</param>
        public IsolatedAsset(string asset, bool isBorrowEnabled, bool isRepayEnabled, decimal borrowed, decimal free, decimal interest, decimal locked, decimal netAsset, decimal netAssetOfBTC, decimal totalAsset)
        {
            Asset = asset;
            IsBorrowEnabled = isBorrowEnabled;
            IsRepayEnabled = isRepayEnabled;
            Borrowed = borrowed;
            Free = free;
            Interest = interest;
            Locked = locked;
            NetAsset = netAsset;
            NetAssetOfBTC = netAssetOfBTC;
            TotalAsset = totalAsset;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"Isolated margin asset {Asset}: Free = {Free}, Borrowed = {Borrowed}, Interest = {Interest}, Locked = {Locked}";
        }
    }
}
