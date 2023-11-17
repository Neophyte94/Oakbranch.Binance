using System;
using System.Collections.Generic;
using Oakbranch.Binance.Models.Filters.Symbol;

namespace Oakbranch.Binance.Models.Futures
{
    /// <summary>
    /// Represents information on a futures contract symbol.
    /// </summary>
    public sealed record SymbolInfo
    {
        /// <summary>
        /// Defines the symbol representing the contract symbol (e.g., BTCUSDT).
        /// </summary>
        public string? Symbol;
        /// <summary>
        /// Defines the symbol representing the underlying trading pair of the contract symbol (e.g., BTCUSDT).
        /// </summary>
        public string? Pair;
        /// <summary>
        /// Defines the type of the contract represented by the symbol.
        /// </summary>
        public ContractType ContractType;
        /// <summary>
        /// Defines the delivery date of the contract represented by the symbol.
        /// </summary>
        public DateTime DeliveryDate;
        /// <summary>
        /// Defines the registration date of the contract represented by the symbol.
        /// </summary>
        public DateTime OnboardDate;
        /// <summary>
        /// Defines the status of the trading pair symbol.
        /// </summary>
        public ContractStatus Status;
        /// <summary>
        /// Defines the base asset of the trading pair.
        /// <para>E.g., for the symbol BTCUSDT the base asset would be BTC.</para>
        /// </summary>
        public string? BaseAsset;
        /// <summary>
        /// Defines the quote asset of the trading pair.
        /// <para>E.g., for the symbol BTCUSDT the quote asset would be USDT.</para>
        /// </summary>
        public string? QuoteAsset;
        /// <summary>
        /// Defines the asset used as a collateral in the contract.
        /// </summary>
        public string? MarginAsset;
        /// <summary>
        /// Defines the precision of the contract price.
        /// <para>Do not use this value as a price step.</para>
        /// </summary>
        public byte PricePrecision;
        /// <summary>
        /// Defines the precision of the contract quantity.
        /// <para>Do not use this value as a lot size step.</para>
        /// </summary>
        public byte QuantityPrecision;
        /// <summary>
        /// Defines the precision of the base asset quantity.
        /// </summary>
        public byte BaseAssetPrecision;
        /// <summary>
        /// Defines the precision of the quote asset price.
        /// </summary>
        public byte QuoteAssetPrecision;
        /// <summary>
        /// Defines the list of order types allowed for the symbol.
        /// </summary>
        public List<OrderType>? OrderTypes;
        /// <summary>
        /// Defines the list of symbol-level filters (constraints).
        /// </summary>
        public List<SymbolFilter>? Filters;
        /// <summary>
        /// Defines the list of time-in-force rules allowed for the symbol.
        /// </summary>
        public List<TimeInForce>? TimeInForceRules;
        /// <summary>
        /// Defines the quote asset value of the contract represented by the symbol.  
        /// <para>The value is <c>Null</c> if not applicable.</para>
        /// </summary>
        public uint? QuoteSize;
        /// <summary>
        /// Defines the threshold for "algo" orders with the price protection enabled.
        /// </summary>
        public decimal ProtectionThreshold;
        /// <summary>
        /// Defines the liquidation fee rate.
        /// </summary>
        public decimal LiquidationFee;
        /// <summary>
        /// Defines the maximum price difference rate (from a mark price) that a market order can make.
        /// </summary>
        public decimal MarketTakeBound;
        /// <summary>
        /// The designation is unknown.
        /// </summary>
        public int MaxMoveOrderLimit;
        /// <summary>
        /// Defines the type of the underlying asset (e.g., "coin").
        /// </summary>
        public string? UnderlyingType;
        /// <summary>
        /// Defines the subtypes of the underlying asset (e.g., "storage").
        /// <para>The value may be <c>Null</c>.</para>
        /// </summary>
        public List<string>? UnderlyingSubtypes;

        /// <summary>
        /// Returns a string representation of the <see cref="SymbolInfo"/> class.
        /// </summary>
        /// <returns>A string representation of the <see cref="SymbolInfo"/> class.</returns>
        public override string ToString()
        {
            return $"Symbol {Symbol}: Type = {ContractType}, Pair = {Pair} Status = {Status}";
        }
    }
}
