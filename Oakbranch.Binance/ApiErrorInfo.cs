using System;

namespace Oakbranch.Binance
{
    public readonly struct ApiErrorInfo
    {
        public readonly int Code;
        public readonly string? Message;

        public ApiErrorInfo(int code, string? message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString()
        {
            return $"{Code}: {Message}";
        }
    }
}
