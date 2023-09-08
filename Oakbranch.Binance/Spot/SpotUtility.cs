using System;
using System.Text.Json;

namespace Oakbranch.Binance.Spot
{
    internal static class SpotUtility
    {
        #region Static methods

        public static string Format(OrderType value)
        {
            switch (value)
            {
                case OrderType.Limit:
                    return "LIMIT";
                case OrderType.LimitMaker:
                    return "LIMIT_MAKER";
                case OrderType.Market:
                    return "MARKET";
                case OrderType.StopLossMarket:
                    return "STOP_LOSS";
                case OrderType.StopLossLimit:
                    return "STOP_LOSS_LIMIT";
                case OrderType.TakeProfitMarket:
                    return "TAKE_PROFIT";
                case OrderType.TakeProfitLimit:
                    return "TAKE_PROFIT_LIMIT";
                default:
                    throw new NotImplementedException($"The order type \"{value}\" is not implemented.");
            }
        }

        public static OrderType ParseOrderType(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException("The order type value is null.");

            switch (s)
            {
                case "LIMIT":
                    return OrderType.Limit;
                case "MARKET":
                    return OrderType.Market;
                case "STOP_LOSS":
                    return OrderType.StopLossMarket;
                case "STOP_LOSS_LIMIT":
                    return OrderType.StopLossLimit;
                case "TAKE_PROFIT":
                    return OrderType.TakeProfitMarket;
                case "TAKE_PROFIT_LIMIT":
                    return OrderType.TakeProfitLimit;
                case "LIMIT_MAKER":
                    return OrderType.LimitMaker;
                default:
                    throw new JsonException($"An unknown order type \"{s}\" was encountered.");
            }
        }

        public static string Format(SelfTradePreventionMode value)
        {
            switch (value)
            {
                case SelfTradePreventionMode.None:
                    return "NONE";
                case SelfTradePreventionMode.ExpireMaker:
                    return "EXPIRE_MAKER";
                case SelfTradePreventionMode.ExpireTaker:
                    return "EXPIRE_TAKER";
                case SelfTradePreventionMode.ExpireBoth:
                    return "EXPIRE_BOTH";
                default:
                    throw new NotImplementedException($"The self trade prevention mode \"{value}\" is not implemented.");
            }
        }

        public static SelfTradePreventionMode ParseSelfTradePreventionMode(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The self trade prevention value is null.");

            switch (s)
            {
                case "NONE":
                    return SelfTradePreventionMode.None;
                case "EXPIRE_MAKER":
                    return SelfTradePreventionMode.ExpireMaker;
                case "EXPIRE_TAKER":
                    return SelfTradePreventionMode.ExpireTaker;
                case "EXPIRE_BOTH":
                    return SelfTradePreventionMode.ExpireBoth;
                default:
                    throw new JsonException($"The self trade prevention mode \"{s}\" is unknown.");
            }
        }

        #endregion
    }
}
