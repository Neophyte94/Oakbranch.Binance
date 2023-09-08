using System;

namespace Oakbranch.Binance.Exceptions
{
    public class QueryInputException : QueryException
    {
        private readonly InputErrorCode m_ErrorCode;
        public InputErrorCode ErrorCode => m_ErrorCode;

        public QueryInputException(InputErrorCode errorCode) : base(FailureReason.InvalidInput)
        {
            m_ErrorCode = errorCode;
        }

        public QueryInputException(InputErrorCode errorCode, string message) : base(FailureReason.InvalidInput, message)
        {
            m_ErrorCode = errorCode;
        }
    }
}
