using System;
using System.Text;

namespace Oakbranch.Binance.Exceptions
{
    /// <summary>
    /// The exception that is thrown when execution of a web query fails.
    /// </summary>
    public class QueryException : Exception
    {
        private readonly FailureReason m_Reason;
        public FailureReason Reason => m_Reason;

        public override String Message
        {
            get
            {
                string s = base.Message;
                if (InnerException != null)
                {
                    s += ": " + InnerException.Message;
                }
                else
                {
                    s += "(" + m_Reason + ")";
                }
                return s;
            }
        }

        public QueryException(FailureReason reason) : this(reason, null) { }

        public QueryException(FailureReason reason, string message) : base(message)
        {
            m_Reason = reason;
        }

        public QueryException(string message, Exception innerException) : base(message, innerException)
        {
            m_Reason = FailureReason.Other;
        }
    }
}
