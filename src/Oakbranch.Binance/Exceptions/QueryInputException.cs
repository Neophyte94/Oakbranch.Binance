using System;

namespace Oakbranch.Binance.Exceptions
{
    public class QueryInputException : QueryException
    {
        public InputErrorCode ErrorCode { get; }

        public QueryInputException(InputErrorCode errorCode) : base(FailureReason.InvalidInput)
        {
            ErrorCode = errorCode;
        }

        public QueryInputException(InputErrorCode errorCode, string message) : base(FailureReason.InvalidInput, message)
        {
            ErrorCode = errorCode;
        }
    }
}
