using System;

namespace Oakbranch.Binance.Models
{
    /// <summary>
    /// Represents information on an asset transfer between accounts.
    /// </summary>
    public readonly struct TransferTransaction
    {
        /// <summary>
        /// Defines the identifier of the transaction.
        /// </summary>
        public readonly long Id;
        /// <summary>
        /// Defines the asset involved in the transaction.
        /// </summary>
        public readonly string Asset;
        /// <summary>
        /// Defines the quantity of the asset involved in the transaction.
        /// </summary>
        public readonly decimal Quantity;
        /// <summary>
        /// Defines the timestamp of the transaction.
        /// </summary>
        public readonly DateTime Timestamp;
        /// <summary>
        /// Defines the status of the transaction.
        /// </summary>
        public readonly TransactionStatus Status;
        /// <summary>
        /// Defines the source account of the transfer.
        /// </summary>
        public readonly AccountType Source;
        /// <summary>
        /// Defines the target account of the transfer.
        /// </summary>
        public readonly AccountType Target;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> struct.
        /// </summary>
        /// <param name="id">The identifier of the transaction.</param>
        /// <param name="asset">The asset involved in the transaction.</param>
        /// <param name="quantity">The quantity of the asset involved in the transaction.</param>
        /// <param name="timestamp">The timestamp of the transaction.</param>
        /// <param name="status">The status of the transaction.</param>
        /// <param name="source">The source account of the transfer.</param>
        /// <param name="target">The target account of the transfer.</param>
        public TransferTransaction(
            long id, string asset, decimal quantity, DateTime timestamp,
            TransactionStatus status, AccountType source, AccountType target)
        {
            Id = id;
            Asset = asset;
            Quantity = quantity;
            Timestamp = timestamp;
            Status = status;
            Source = source;
            Target = target;
        }

        public override string ToString()
        {
            return $"Asset transfer: Source = {Source}, Target = {Target}, Status = {Status}, Asset = {Asset}, Quantity = {Quantity}, Time = {Timestamp}";
        }
    }
}