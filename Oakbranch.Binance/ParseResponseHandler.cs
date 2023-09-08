using System;

namespace Oakbranch.Binance
{
    internal delegate T ParseResponseHandler<out T>(byte[] data, object args);
}
