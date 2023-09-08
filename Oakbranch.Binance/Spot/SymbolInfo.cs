using System;
using System.Collections.Generic;
using Oakbranch.Binance.Filters.Symbol;

namespace Oakbranch.Binance.Spot
{
    /// <summary>
    /// Represents information on a trading pair in the spot market.
    /// </summary>
    public sealed class SymbolInfo
    {
        /// <summary>
        /// Defines the symbol representing the trading pair (e.g., BTCUSDT).
        /// </summary>
        public string Symbol;
        /// <summary>
        /// Defines the status of the trading pair symbol.
        /// </summary>
        public SymbolStatus Status;
        /// <summary>
        /// Defines the base asset of the trading pair symbol.
        /// <para>E.g., for the symbol BTCUSDT the base asset would be BTC.</para>
        /// </summary>
        public string BaseAsset;
        /// <summary>
        /// Defines the quote asset of the trading pair symbol.
        /// <para>E.g., for the symbol BTCUSDT the quote asset would be USDT.</para>
        /// </summary>
        public string QuoteAsset;
        /// <summary>
        /// Defines the precision of the base asset quantity.
        /// </summary>
        public byte BaseAssetPrecision;
        /// <summary>
        /// Defines the precision of the quote asset price.
        /// </summary>
        public byte QuoteAssetPrecision;
        /// <summary>
        /// Defines the precision of the base asset commission.
        /// </summary>
        public byte BaseComissionPrecision;
        /// <summary>
        /// Defines the precision of the quote asset commission.
        /// </summary>
        public byte QuoteComissionPrecision;
        /// <summary>
        /// Defines the list of order types allowed for the trading pair.
        /// </summary>
        public List<OrderType> OrderTypes;
        /// <summary>
        /// Defines the list of symbol-level filters (restrictions).
        /// </summary>
        public List<SymbolFilter> Filters;
        /// <summary>
        /// Defines the permissions for the trading pair.
        /// </summary>
        public SymbolPermissions Permissions;
        /// <summary>
        /// Defines the default self-trade prevention mode used in the spot market.
        /// </summary>
        public SelfTradePreventionMode DefaultSTPMode;
        /// <summary>
        /// Defines self-trade prevention modes available to a user.
        /// </summary>
        public List<SelfTradePreventionMode> AllowedSTPModes;

        /// <summary>
        /// Returns a string representation of the <see cref="SymbolInfo"/> class.
        /// </summary>
        /// <returns>A string representation of the <see cref="SymbolInfo"/> class.</returns>
        public override string ToString()
        {
            return $"Symbol {Symbol}: Status = {Status}, Base asset precision = {BaseAssetPrecision}, Quote asset precision = {QuoteAssetPrecision}";
        }
    }
}
