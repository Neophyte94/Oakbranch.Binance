using System;

namespace Oakbranch.Binance.Margin
{
    public abstract record MarginOrderResponseBase : PostOrderResponseBase
    {
        /// <summary>
        /// Gets the type of the response.
        /// </summary>
        public abstract OrderResponseType Type { get; }
        /// <summary>
        /// Defines the type of a margin account the order was posted from.
        /// <para>The value <see langword="true"/> for an isolated margin account, <see langword="false"/> for the cross margin account.</para>
        /// </summary>
        public bool IsIsolated;
    }
}
