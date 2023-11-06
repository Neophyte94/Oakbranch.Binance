using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Represents information on repayment of a margin asset.
    /// </summary>
    public readonly struct RepayTransaction
    {
        public readonly long Id;
        /// <summary>
        /// Defines the isolated margin symbol that the transaction was made for.
        /// <para>The value is <c>Null</c> for non-isolated margin accounts.</para>
        /// </summary>
        public readonly string IsolatedSymbol;
        /// <summary>
        /// Defines the asset that was repaid or requested to.
        /// </summary>
        public readonly string Asset;
        /// <summary>
        /// Defines the total quantity of the asset repaid or requested to.
        /// </summary>
        public readonly decimal Quantity;
        /// <summary>
        /// Defines the principal quantity repaid.
        /// </summary>
        public readonly decimal Principal;
        /// <summary>
        /// Defines the interest quantity repaid.
        /// </summary>
        public readonly decimal Interest;
        /// <summary>
        /// Defines the date &amp; time when the repayment was made.
        /// </summary>
        public readonly DateTime Timestamp;
        /// <summary>
        /// Defines the status of the repayment transaction.
        /// </summary>
        public readonly TransactionStatus Status;

        /// <summary>
        /// Creates a new instance of the <see cref="RepayTransaction"/> struct.
        /// </summary>
        /// <param name="id">The identifier of the repayment transaction.</param>
        /// <param name="isolatedSymbol">
        /// The isolated margin symbol that the repayment transaction was made for.
        /// <para>Use the <c>Null</c> value for a non-isolated margin account.</para>
        /// </param>
        /// <param name="asset">The asset that was repaid or requested to.</param>
        /// <param name="quantity">The amount of the repayment transaction.</param>
        /// <param name="principal">The principal amount of the repayment transaction.</param>
        /// <param name="interest">The interest amount of the repayment transaction.</param>
        /// <param name="timestamp">The date and time when the repayment transaction occurred.</param>
        /// <param name="status">The status of the repayment transaction.</param>
        public RepayTransaction(long id, string isolatedSymbol, string asset, decimal quantity, decimal principal, decimal interest, DateTime timestamp, TransactionStatus status)
        {
            Id = id;
            IsolatedSymbol = isolatedSymbol;
            Asset = asset;
            Quantity = quantity;
            Principal = principal;
            Interest = interest;
            Timestamp = timestamp;
            Status = status;
        }
    }
}
