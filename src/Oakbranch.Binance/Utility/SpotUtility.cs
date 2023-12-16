using System;
using System.Text.Json;
using Oakbranch.Binance.Models.Spot;

namespace Oakbranch.Binance.Utility;

internal static class SpotUtility
{
    public static string Format(KlineInterval value)
    {
        return value switch
        {
            KlineInterval.Second1 => "1s",
            KlineInterval.Minute1 => "1m",
            KlineInterval.Minute3 => "3m",
            KlineInterval.Minute5 => "5m",
            KlineInterval.Minute15 => "15m",
            KlineInterval.Minute30 => "30m",
            KlineInterval.Hour1 => "1h",
            KlineInterval.Hour2 => "2h",
            KlineInterval.Hour4 => "4h",
            KlineInterval.Day1 => "1d",
            KlineInterval.Week1 => "1w",
            KlineInterval.Hour6 => "6h",
            KlineInterval.Hour8 => "8h",
            KlineInterval.Hour12 => "12h",
            KlineInterval.Day3 => "3d",
            KlineInterval.Month1 => "1M",
            _ => throw new NotImplementedException($"The interval \"{value}\" is not implemented."),
        };
    }

    public static SymbolStatus ParseSymbolStatus(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("The symbol status value is null");
        }

        return s switch
        {
            "TRADING" => SymbolStatus.Trading,
            "BREAK" => SymbolStatus.Break,
            "PRE_TRADING" => SymbolStatus.PreTrading,
            "POST_TRADING" => SymbolStatus.PostTrading,
            "END_OF_DAY" => SymbolStatus.EndOfDay,
            "HALT" => SymbolStatus.Halt,
            "AUCTION_MATCH" => SymbolStatus.AuctionMatch,
            _ => throw new JsonException($"The symbol status \"{s}\" is unknown."),
        };
    }

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
        {
            throw new JsonException($"The order type value is null or empty.");
        }

        return s switch
        {
            "LIMIT" => OrderType.Limit,
            "LIMIT_MAKER" => OrderType.LimitMaker,
            "MARKET" => OrderType.Market,
            "STOP_LOSS" => OrderType.StopLossMarket,
            "STOP_LOSS_LIMIT" => OrderType.StopLossLimit,
            "TAKE_PROFIT" => OrderType.TakeProfitMarket,
            "TAKE_PROFIT_LIMIT" => OrderType.TakeProfitLimit,
            _ => throw new JsonException($"The order type \"{s}\" is unknown."),
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

    public static string Format(TimeInForce value)
    {
        return value switch
        {
            TimeInForce.GoodTillCanceled => "GTC",
            TimeInForce.FillOrKill => "FOK",
            TimeInForce.ImmediateOrCancel => "IOC",
            _ => throw new NotImplementedException($"The time-in-force type \"{value}\" is not implemented."),
        };
    }

    public static TimeInForce ParseTimeInForce(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new JsonException($"The time in force rule value is null.");

        return s switch
        {
            "GTC" => TimeInForce.GoodTillCanceled,
            "IOC" => TimeInForce.ImmediateOrCancel,
            "FOK" => TimeInForce.FillOrKill,
            _ => throw new JsonException($"An unknown time in force rule \"{s}\" was encountered."),
        };
    }

    public static string Format(CancellationRestriction value)
    {
        return value switch
        {
            CancellationRestriction.OnlyNew => "ONLY_NEW",
            CancellationRestriction.OnlyPartiallyFilled => "ONLY_PARTIALLY_FILLED",
            _ => throw new NotImplementedException($"The cancellation restriction rule \"{value}\" is not implemented."),
        };
    }

    public static string Format(OrderResponseType value)
    {
        return value switch
        {
            OrderResponseType.Ack => "ACK",
            OrderResponseType.Full => "FULL",
            OrderResponseType.Result => "RESULT",
            _ => throw new NotImplementedException($"The order response type \"{value}\" is not implemented."),
        };
    }
}
