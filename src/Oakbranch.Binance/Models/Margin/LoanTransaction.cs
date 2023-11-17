using System;

namespace Oakbranch.Binance.Models.Margin
{
    /// <summary>
    /// Represents information on borrowing a margin asset.
    /// </summary>
    public readonly struct LoanTransaction
    {
        /// <summary>
        /// Defines the identifier of the transaction.
        /// </summary>
        public readonly long Id;
        /// <summary>
        /// Defines the isolated margin symbol that the transaction was made for.
        /// <para>The value is <c>Null</c> for non-isolated margin accounts.</para>
        /// </summary>
        public readonly string IsolatedSymbol;
        /// <summary>
        /// Defines the asset that was borrowed or requested to.
        /// </summary>
        public readonly string Asset;
        /// <summary>
        /// Defines the quantity of the loan principal.
        /// </summary>
        public readonly decimal Principal;
        /// <summary>
        /// Defines the time when the transaction was made.
        /// </summary>
        public readonly DateTime Timestamp;
        /// <summary>
        /// Defines the status of the transaction.
        /// </summary>
        public readonly TransactionStatus Status;

        /// <summary>
        /// Creates a new instance of the <see cref="LoanTransaction"/> struct.
        /// </summary>
        /// <param name="id">The identifier of the transaction.</param>
        /// <param name="isolatedSymbol">
        /// The isolated margin symbol that the transaction was made for.
        /// <para>Use the <c>null</c> value for a non-isolated margin account.</para>
        /// </param>
        /// <param name="asset">The asset associated with the transaction.</param>
        /// <param name="principal">The principal amount of the transaction.</param>
        /// <param name="timestamp">The timestamp of the transaction.</param>
        /// <param name="status">The status of the transaction.</param>
        public LoanTransaction(long id, string isolatedSymbol, string asset, decimal principal, DateTime timestamp, TransactionStatus status)
        {
            Id = id;
            IsolatedSymbol = isolatedSymbol;
            Asset = asset;
            Principal = principal;
            Timestamp = timestamp;
            Status = status;
        }
    }
}
