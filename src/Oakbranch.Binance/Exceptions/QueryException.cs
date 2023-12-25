using System;
using System.Text;
using Oakbranch.Binance.Core;

namespace Oakbranch.Binance.Exceptions
{
    /// <summary>
    /// The exception that is thrown when execution of a web query fails.
    /// </summary>
    public class QueryException : Exception
    {
        public FailureReason Reason { get; }

        public override string Message
        {
            get
            {
                string s = base.Message;
                if (InnerException != null)
                {
                    return s + ": " + InnerException.Message;
                }
                else
                {
                    return s + " (" + Reason + ")";
                }
            }
        }

        public QueryException(FailureReason reason) : this(reason, null)
        { }

        public QueryException(FailureReason reason, string? message) : base(message)
        {
            Reason = reason;
        }

        public QueryException(string? message, Exception innerException) : base(message, innerException)
        {
            Reason = FailureReason.Other;
        }
    }
}
