﻿using System;

namespace Oakbranch.Binance.Models.Margin
{
    /// <summary>
    /// Represents a user trade made from a margin account.
    /// </summary>
    public struct MarginTrade
    {
        /// <summary>
        /// Defines the symbol that the trade was made on.
        /// </summary>
        public string Symbol;
        /// <summary>
        /// Defines the identifier of the trade.
        /// </summary>
        public long Id;
        /// <summary>
        /// Defines the identifier of the order associated with the trade.
        /// </summary>
        public long OrderId;
        /// <summary>
        /// Defines the price at which the trade was executed.
        /// </summary>
        public decimal Price;
        /// <summary>
        /// Defines the quantity of the base asset traded.
        /// </summary>
        public decimal Quantity;
        /// <summary>
        /// Defines the quantity of the quote asset traded.
        /// </summary>
        public decimal QuoteQuantity;
        /// <summary>
        /// Defines the commission amount charged for the trade.
        /// </summary>
        public decimal Commission;
        /// <summary>
        /// Defines the asset in which the commission was charged.
        /// </summary>
        public string CommissionAsset;
        /// <summary>
        /// Defines the timestamp of the trade.
        /// </summary>
        public DateTime Time;
        /// <summary>
        /// Defines whether the trade was executed as a buyer.
        /// </summary>
        public bool IsBuyer;
        /// <summary>
        /// Defines whether the trade was executed as a maker.
        /// </summary>
        public bool IsMaker;
        /// <summary>
        /// Defines whether the trade was executed as the best match.
        /// </summary>
        public bool IsBestMatch;
        /// <summary>
        /// Indicates the type of the account the trade was made from.
        /// <para>The value is <see langword="true"/> for an isolated margin accounts, and <see langword="false"/> for the cross margin account.</para>
        /// </summary>
        public bool IsIsolated;
    }
}