using System;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.UnitTests
{
    public interface IApiConnectorFactory : IDisposable
    {
        IApiConnector Create();
    }
}
