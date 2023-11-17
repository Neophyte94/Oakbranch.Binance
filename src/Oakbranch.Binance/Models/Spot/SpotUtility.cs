using System;
using System.Text.Json;

namespace Oakbranch.Binance.Models.Spot
{
    internal static class SpotUtility
    {
        #region Static methods

        public static string Format(OrderType value)
        {
            return value switch
            {
                OrderType.Limit => "LIMIT",
                OrderType.LimitMaker => "LIMIT_MAKER",
                OrderType.Market => "MARKET",
                OrderType.StopLossMarket => "STOP_LOSS",
                OrderType.StopLossLimit => "STOP_LOSS_LIMIT",
                OrderType.TakeProfitMarket => "TAKE_PROFIT",
                OrderType.TakeProfitLimit => "TAKE_PROFIT_LIMIT",
                _ => throw new NotImplementedException($"The order type \"{value}\" is not implemented."),
            };
        }

        public static OrderType ParseOrderType(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new JsonException("The order type value is null.");

            return s switch
            {
                "LIMIT" => OrderType.Limit,
                "MARKET" => OrderType.Market,
                "STOP_LOSS" => OrderType.StopLossMarket,
                "STOP_LOSS_LIMIT" => OrderType.StopLossLimit,
                "TAKE_PROFIT" => OrderType.TakeProfitMarket,
                "TAKE_PROFIT_LIMIT" => OrderType.TakeProfitLimit,
                "LIMIT_MAKER" => OrderType.LimitMaker,
                _ => throw new JsonException($"An unknown order type \"{s}\" was encountered."),
            };
        }

        public static string Format(SelfTradePreventionMode value)
        {
            return value switch
            {
                SelfTradePreventionMode.None => "NONE",
                SelfTradePreventionMode.ExpireMaker => "EXPIRE_MAKER",
                SelfTradePreventionMode.ExpireTaker => "EXPIRE_TAKER",
                SelfTradePreventionMode.ExpireBoth => "EXPIRE_BOTH",
                _ => throw new NotImplementedException($"The self trade prevention mode \"{value}\" is not implemented."),
            };
        }

        public static SelfTradePreventionMode ParseSelfTradePreventionMode(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new JsonException($"The self trade prevention value is null.");

            return s switch
            {
                "NONE" => SelfTradePreventionMode.None,
                "EXPIRE_MAKER" => SelfTradePreventionMode.ExpireMaker,
                "EXPIRE_TAKER" => SelfTradePreventionMode.ExpireTaker,
                "EXPIRE_BOTH" => SelfTradePreventionMode.ExpireBoth,
                _ => throw new JsonException($"The self trade prevention mode \"{s}\" is unknown."),
            };
        }

        #endregion
    }
}
