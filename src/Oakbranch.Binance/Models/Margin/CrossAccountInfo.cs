using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Models.Margin
{
    /// <summary>
    /// Represents information on the state of the cross margin account.
    /// </summary>
    public sealed class CrossAccountInfo
    {
        /// <summary>
        /// Defines a list of user assets in the cross margin account.
        /// </summary>
        public List<CrossAsset>? UserAssets;
        /// <summary>
        /// Defines the total value of all owned assets in the account, denominated in Bitcoin.
        /// </summary>
        public double TotalAssetOfBTC;
        /// <summary>
        /// Defines the total value of the liabilities in the account, denominated in Bitcoin.
        /// </summary>
        public double TotalLiabilityOfBTC;
        /// <summary>
        /// Defines the total net asset value of the account, denominated in Bitcoin.
        /// </summary>
        public double TotalNetAssetOfBTC;
        /// <summary>
        /// Defines whether trading is enabled in the account.
        /// </summary>
        public bool IsTradeEnabled;
        /// <summary>
        /// Defines whether transferring funds is enabled in the account.
        /// </summary>
        public bool IsTransferEnabled;
        /// <summary>
        /// Defines whether borrowing is enabled in the account.
        /// </summary>
        public bool IsBorrowEnabled;
        /// <summary>
        /// Defines the current margin level of the account.
        /// </summary>
        public double MarginLevel;
    }
}
