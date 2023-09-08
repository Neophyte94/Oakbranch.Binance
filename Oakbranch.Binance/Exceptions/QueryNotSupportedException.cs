using System;

namespace Oakbranch.Binance.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a secured query is sent
    /// via an <see cref="IApiConnector"/> instance that does not support digital signing.
    /// <para>A secured query is a web query that is expected to be digitally signed with a secret API key.</para>
    /// </summary>
    public class QueryNotSupportedException : NotSupportedException
    {
        public QueryNotSupportedException() : base() { }

        public QueryNotSupportedException(string message) : base(message) { }
    }
}
