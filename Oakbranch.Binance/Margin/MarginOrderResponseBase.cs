using System;

namespace Oakbranch.Binance.Margin
{
    public abstract class MarginOrderResponseBase : PostOrderResponseBase
    {
        /// <summary>
        /// Gets the type of the response.
        /// </summary>
        public abstract OrderResponseType Type { get; }
        /// <summary>
        /// Defines the type of a margin account the order was posted from.
        /// <para>The value <c>True</c> for an isolated margin account, <c>False</c> for the cross margin account.</para>
        /// </summary>
        public bool IsIsolated;
    }
}
