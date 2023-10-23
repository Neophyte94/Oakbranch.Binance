using System;

namespace Oakbranch.Binance.UnitTests
{
    public interface IApiConnectorFactory : IDisposable
    {
        IApiConnector Create();
    }
}
