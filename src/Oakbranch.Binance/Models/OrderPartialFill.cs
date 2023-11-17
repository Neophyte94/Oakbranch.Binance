using System;

namespace Oakbranch.Binance.Models
{
    /// <summary>
    /// Represents a partial fill of an order.
    /// </summary>
    public readonly struct OrderPartialFill
    {
        /// <summary>
        /// Defines the identifier of the trade associated with the partial fill.
        /// </summary>
        public readonly long TradeId;

        /// <summary>
        /// Defines the price of the partial fill.
        /// </summary>
        public readonly decimal Price;

        /// <summary>
        /// Defines the quantity of the partial fill.
        /// </summary>
        public readonly decimal Quantity;

        /// <summary>
        /// Defines the commission for the partial fill.
        /// </summary>
        public readonly decimal Commission;

        /// <summary>
        /// Defines the asset used for commission for the partial fill.
        /// </summary>
        public readonly string CommissionAsset;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderPartialFill"/> struct.
        /// </summary>
        /// <param name="tradeId">The identifier of the trade associated with the partial fill.</param>
        /// <param name="price">The price of the partial fill.</param>
        /// <param name="quantity">The quantity of the partial fill.</param>
        /// <param name="commission">The commission for the partial fill.</param>
        /// <param name="commissionAsset">The asset used for commission for the partial fill.</param>
        public OrderPartialFill(long tradeId, decimal price, decimal quantity, decimal commission, string commissionAsset)
        {
            TradeId = tradeId;
            Price = price;
            Quantity = quantity;
            Commission = commission;
            CommissionAsset = commissionAsset;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="OrderPartialFill"/> struct.
        /// </summary>
        /// <returns>A string representation of the <see cref="OrderPartialFill"/> struct.</returns>
        public override string ToString()
        {
            return $"Partial fill: Price = {Price}, Quantity = {Quantity}, Commission = {Commission}, Commission Asset = {CommissionAsset}";
        }
    }
}
