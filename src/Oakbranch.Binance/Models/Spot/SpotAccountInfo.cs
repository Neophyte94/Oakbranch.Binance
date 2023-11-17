using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Models.Spot
{
    /// <summary>
    /// Represents information on the state of a spot account.
    /// </summary>
    public sealed record SpotAccountInfo
    {
        /// <summary>
        /// Defines the rate of a commission applied to every maker trade.
        /// <para>A maker trade is the one executed from an order put in the order book (e.g., "limit" orders).</para>
        /// </summary>
        public decimal MakerCommissionRate;

        /// <summary>
        /// Defines the rate of a commission applied to every taker trade.
        /// <para>A taker trade is the one executed from an order, either fully or partially, before it goes on the order book 
        /// (i.e., all "market" orders, including IOC and FOK ones).</para>
        /// </summary>
        public decimal TakerCommissionRate;

        /// <summary>
        /// Defines the rate of a commission applied to every buyer trade.
        /// </summary>
        public decimal BuyerCommissionRate;

        /// <summary>
        /// Defines the rate of a commission applied to every seller trade.
        /// </summary>
        public decimal SellerCommissionRate;

        /// <summary>
        /// Defines whether the account is allowed to trade.
        /// </summary>
        public bool CanTrade;

        /// <summary>
        /// Defines whether the account is allowed to withdraw.
        /// </summary>
        public bool CanWithdraw;

        /// <summary>
        /// Defines whether the account is allowed to deposit.
        /// </summary>
        public bool CanDeposit;

        /// <summary>
        /// Defines whether the account is brokered.
        /// </summary>
        public bool IsBrokered;

        /// <summary>
        /// Defines whether the account requires self-trade prevention.
        /// </summary>
        public bool RequiresSelfTradePrevention;

        /// <summary>
        /// Defines the update time of the account information.
        /// </summary>
        public DateTime UpdateTime;

        /// <summary>
        /// Defines the type of the account.
        /// </summary>
        public string? AccountType;

        /// <summary>
        /// Defines the balances of the spot assets in the account.
        /// </summary>
        public List<SpotAsset>? Balances;

        /// <summary>
        /// Defines the permissions of the account.
        /// </summary>
        public List<string>? Permissions;
    }
}
