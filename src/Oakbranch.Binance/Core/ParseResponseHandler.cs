﻿namespace Oakbranch.Binance.Core
{
    internal delegate T ParseResponseHandler<out T>(
        byte[] data,
        object? args);
}