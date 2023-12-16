using System;
using System.Text.Json;
using Oakbranch.Binance.Models;
using Oakbranch.Binance.Models.Margin;

namespace Oakbranch.Binance.Utility;

internal static class MarginUtility
{
    public static TransferDirection ParseTransferDirection(string s)
    {
        return s switch
        {
            "ROLL_IN" => TransferDirection.RollIn,
            "ROLL_OUT" => TransferDirection.RollOut,
            _ => throw new JsonException($"The transfer direction \"{s}\" is unknown."),
        };
    }

    public static string Format(TransferDirection value)
    {
        if (value == TransferDirection.RollIn)
        {
            return "ROLL_IN";
        }
        else if (value == TransferDirection.RollOut)
        {
            return "ROLL_OUT";
        }
        else
        {
            throw new NotImplementedException($"The transfer direction \"{value}\" is not implemented.");
        }
    }

    public static AccountType ParseAccountType(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("The account type value is null or empty.");
        }

        return s switch
        {
            "SPOT" => AccountType.Spot,
            "ISOLATED_MARGIN" => AccountType.IsolatedMargin,
            _ => throw new JsonException($"The unknown account type \"{s}\" was encountered."),
        };
    }

    public static string Format(AccountType value)
    {
        return value switch
        {
            AccountType.Spot => "SPOT",
            AccountType.IsolatedMargin => "ISOLATED_MARGIN",
            _ => throw new NotSupportedException($"The account type \"{value}\" is not supported."),
        };
    }

    public static TimeInForce ParseTimeInForce(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("The time in force value is null or empty.");
        }

        return s switch
        {
            "GTC" => TimeInForce.GoodTillCanceled,
            "IOC" => TimeInForce.ImmediateOrCancel,
            "FOK" => TimeInForce.FillOrKill,
            _ => throw new JsonException($"An unknown time in force rule \"{s}\" was encountered."),
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

    public static OrderType ParseOrderType(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("The order type value is null or empty.");
        }

        return s switch
        {
            "LIMIT" => OrderType.Limit,
            "LIMIT_MAKER" => OrderType.LimitMaker,
            "MARKET" => OrderType.Market,
            "STOP_LOSS" => OrderType.StopLoss,
            "STOP_LOSS_LIMIT" => OrderType.StopLossLimit,
            "TAKE_PROFIT" => OrderType.TakeProfit,
            "TAKE_PROFIT_LIMIT" => OrderType.TakeProfitLimit,
            _ => throw new JsonException($"The order type \"{s}\" is unknown."),
        };
    }

    public static string Format(OrderType value)
    {
        return value switch
        {
            OrderType.Limit => "LIMIT",
            OrderType.LimitMaker => "LIMIT_MAKER",
            OrderType.Market => "MARKET",
            OrderType.StopLoss => "STOP_LOSS",
            OrderType.StopLossLimit => "STOP_LOSS_LIMIT",
            OrderType.TakeProfit => "TAKE_PROFIT",
            OrderType.TakeProfitLimit => "TAKE_PROFIT_LIMIT",
            _ => throw new NotImplementedException($"The order type \"{value}\" is not implemented."),
        };
    }

    public static MarginStatus ParseMarginStatus(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("The margin status value is null or empty.");
        }

        return s switch
        {
            "EXCESSIVE" => MarginStatus.Excessive,
            "NORMAL" => MarginStatus.Normal,
            "MARGIN_CALL" => MarginStatus.MarginCall,
            "PRE_LIQUIDATION" => MarginStatus.PreLiquidation,
            "FORCE_LIQUIDATION" => MarginStatus.ForceLiquidation,
            _ => throw new JsonException($"An unknown margin status \"{s}\" was encountered."),
        };
    }

    public static TransactionStatus ParseTransactionStatus(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("The transaction status value is null or empty.");
        }

        return s switch
        {
            "PENDING" => TransactionStatus.Pending,
            "CONFIRMED" or "CONFIRM" => TransactionStatus.Confirmed,
            "FAILED" => TransactionStatus.Failed,
            _ => throw new JsonException($"An unknown transaction status \"{s}\" was encountered."),
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

    public static string Format(MarginSideEffect value)
    {
        return value switch
        {
            MarginSideEffect.NoSideEffect => "NO_SIDE_EFFECT",
            MarginSideEffect.MarginBuy => "MARGIN_BUY",
            MarginSideEffect.AutoRepay => "AUTO_REPAY",
            _ => throw new NotImplementedException($"The margin side effect type \"{value}\" is not implemented."),
        };
    }
}