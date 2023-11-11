using System;

namespace Oakbranch.Binance.Exceptions
{
    public class QueryInputException : QueryException
    {
        #region Instance members

        public InputErrorCode ErrorCode { get; }

        #endregion

        #region Instance constructors

        public QueryInputException(InputErrorCode errorCode) : base(FailureReason.InvalidInput)
        {
            ErrorCode = errorCode;
        }

        public QueryInputException(InputErrorCode errorCode, string message) : base(FailureReason.InvalidInput, message)
        {
            ErrorCode = errorCode;
        }

        #endregion
    }
}
