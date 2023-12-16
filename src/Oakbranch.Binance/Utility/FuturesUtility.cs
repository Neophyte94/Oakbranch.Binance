using System;
using System.Text.Json;
using Oakbranch.Binance.Models.Futures;

namespace Oakbranch.Binance.Utility;

internal static class FuturesUtility
{
    public static string Format(ContractType value)
    {
        return value switch
        {
            ContractType.Perpetual => "PERPETUAL",
            ContractType.CurrentMonth => "CURRENT_MONTH",
            ContractType.NextMonth => "NEXT_MONTH",
            ContractType.CurrentQuarter => "CURRENT_QUARTER",
            ContractType.NextQuarter => "NEXT_QUARTER",
            ContractType.PerpetualDelivering => "PERPETUAL_DELIVERING",
            _ => throw new NotImplementedException($"The contract type \"{value}\" is not implemented."),
        };
    }

    public static ContractType ParseContractType(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new JsonException($"The contract type value is null.");

        return s switch
        {
            "PERPETUAL" => ContractType.Perpetual,
            "CURRENT_MONTH" => ContractType.CurrentMonth,
            "NEXT_MONTH" => ContractType.NextMonth,
            "CURRENT_QUARTER" or "CURRENT_QUARTER_DELIVERING" => ContractType.CurrentQuarter,
            "NEXT_QUARTER" or "NEXT_QUARTER_DELIVERING" => ContractType.NextQuarter,
            "PERPETUAL_DELIVERING" or "PERPETUAL DELIVERING" => ContractType.PerpetualDelivering,
            _ => throw new JsonException($"An unknown contract type \"{s}\" was encountered."),
        };
    }

    public static string Format(ContractStatus value)
    {
        return value switch
        {
            ContractStatus.PendingTrading => "PENDING_TRADING",
            ContractStatus.Trading => "TRADING",
            ContractStatus.PreDelivering => "PRE_DELIVERING",
            ContractStatus.Delivering => "DELIVERING",
            ContractStatus.Delivered => "DELIVERED",
            ContractStatus.PreSettle => "PRE_SETTLE",
            ContractStatus.Settling => "SETTLING",
            ContractStatus.Close => "CLOSE",
            _ => throw new NotImplementedException($"The contract status \"{value}\" is not implemented."),
        };
    }

    public static ContractStatus ParseContractStatus(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new JsonException($"The contract status value is null.");

        return s switch
        {
            "PENDING_TRADING" => ContractStatus.PendingTrading,
            "TRADING" => ContractStatus.Trading,
            "PRE_DELIVERING" => ContractStatus.PreDelivering,
            "DELIVERING" => ContractStatus.Delivering,
            "DELIVERED" => ContractStatus.Delivered,
            "PRE_SETTLE" => ContractStatus.PreSettle,
            "SETTLING" => ContractStatus.Settling,
            "CLOSE" => ContractStatus.Close,
            _ => throw new JsonException($"An unknown contract status \"{s}\" was encountered."),
        };
    }

    public static string Format(OrderType value)
    {
        return value switch
        {
            OrderType.Limit => "LIMIT",
            OrderType.Market => "MARKET",
            OrderType.StopLossLimit => "STOP",
            OrderType.StopLossMarket => "STOP_MARKET",
            OrderType.TakeProfitLimit => "TAKE_PROFIT",
            OrderType.TakeProfitMarket => "TAKE_PROFIT_MARKET",
            OrderType.TrailingStopMarket => "TRAILING_STOP_MARKET",
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
            "STOP" => OrderType.StopLossLimit,
            "STOP_MARKET" => OrderType.StopLossMarket,
            "TAKE_PROFIT" => OrderType.TakeProfitLimit,
            "TAKE_PROFIT_MARKET" => OrderType.TakeProfitMarket,
            "TRAILING_STOP_MARKET" => OrderType.TrailingStopMarket,
            _ => throw new JsonException($"An unknown order type \"{s}\" was encountered."),
        };
    }

    public static string Format(TimeInForce value)
    {
        return value switch
        {
            TimeInForce.GoodTillCanceled => "GTC",
            TimeInForce.FillOrKill => "FOK",
            TimeInForce.ImmediateOrCancel => "IOC",
            TimeInForce.GoodTillCrossing => "GTX",
            TimeInForce.GoodTillDate => "GTD",
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
            "GTX" => TimeInForce.GoodTillCrossing,
            "GTD" => TimeInForce.GoodTillDate,
            _ => throw new JsonException($"An unknown time in force rule \"{s}\" was encountered."),
        };
    }

    public static string Format(OrderResponseType value)
    {
        return value switch
        {
            OrderResponseType.Ack => "ACK",
            OrderResponseType.Result => "RESULT",
            _ => throw new NotImplementedException($"The order response type \"{value}\" is not implemented."),
        };
    }

    public static OrderResponseType ParseOrderResponseType(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new JsonException($"The order response type value is null.");

        return s switch
        {
            "ACK" => OrderResponseType.Ack,
            "RESULT" => OrderResponseType.Result,
            _ => throw new JsonException($"An unknown order response type \"{s}\" was encountered."),
        };
    }

    public static string Format(KlineInterval value)
    {
        return value switch
        {
            KlineInterval.Minute1 => "1m",
            KlineInterval.Minute3 => "3m",
            KlineInterval.Minute5 => "5m",
            KlineInterval.Minute15 => "15m",
            KlineInterval.Minute30 => "30m",
            KlineInterval.Hour1 => "1h",
            KlineInterval.Hour2 => "2h",
            KlineInterval.Hour4 => "4h",
            KlineInterval.Hour6 => "6h",
            KlineInterval.Hour8 => "8h",
            KlineInterval.Hour12 => "12h",
            KlineInterval.Day1 => "1d",
            KlineInterval.Week1 => "1w",
            KlineInterval.Day3 => "3d",
            KlineInterval.Month1 => "1M",
            _ => throw new NotImplementedException($"The kline interval \"{value}\" is not implemented."),
        };
    }

    public static string Format(StatsInterval value)
    {
        return value switch
        {
            StatsInterval.Minute5 => "5m",
            StatsInterval.Minute15 => "15m",
            StatsInterval.Minute30 => "30m",
            StatsInterval.Hour1 => "1h",
            StatsInterval.Hour2 => "2h",
            StatsInterval.Hour4 => "4h",
            StatsInterval.Hour6 => "6h",
            StatsInterval.Hour12 => "12h",
            StatsInterval.Day1 => "1d",
            _ => throw new NotImplementedException($"The stats interval \"{value}\" is not implemented."),
        };
    }
}
