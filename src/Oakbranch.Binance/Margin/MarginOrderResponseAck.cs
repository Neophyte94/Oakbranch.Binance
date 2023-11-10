using System;

namespace Oakbranch.Binance.Margin
{
    /// <summary>
    /// Represents a response of the type <see cref="OrderResponseType.Ack"/> to a post order request.
    /// </summary>
    public sealed record MarginOrderResponseAck : MarginOrderResponseBase
    {
        public override OrderResponseType Type => OrderResponseType.Ack;
    }
}
