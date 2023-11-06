using System;

namespace Oakbranch.Binance.Spot
{
    /// <summary>
    /// Represents a response of the type <see cref="OrderResponseType.Ack"/> to a post order request.
    /// </summary>
    public sealed class SpotOrderResponseAck : SpotOrderResponseBase
    {
        public override OrderResponseType Type => OrderResponseType.Ack;
    }
}
