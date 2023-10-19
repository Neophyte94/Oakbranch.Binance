using System;

namespace Oakbranch.Binance.UnitTests
{
    public interface IApiConnectorFactory
    {
        IApiConnector Create();
    }
}
