using System;
using System.Text.Json;

namespace Oakbranch.Binance.Futures
{
    internal static class FuturesUtility
    {
        #region Static methods

        public static string Format(ContractType value)
        {
            switch (value)
            {
                case ContractType.Perpetual:
                    return "PERPETUAL";
                case ContractType.CurrentMonth:
                    return "CURRENT_MONTH";
                case ContractType.NextMonth:
                    return "NEXT_MONTH";
                case ContractType.CurrentQuarter:
                    return "CURRENT_QUARTER";
                case ContractType.NextQuarter:
                    return "NEXT_QUARTER";
                case ContractType.PerpetualDelivering:
                    return "PERPETUAL_DELIVERING";
                default:
                    throw new NotImplementedException($"The contract type \"{value}\" is not implemented.");
            }
        }

        public static ContractType ParseContractType(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The contract type value is null.");

            switch (s)
            {
                case "PERPETUAL":
                    return ContractType.Perpetual;
                case "CURRENT_MONTH":
                    return ContractType.CurrentMonth;
                case "NEXT_MONTH":
                    return ContractType.NextMonth;
                case "CURRENT_QUARTER":
                case "CURRENT_QUARTER_DELIVERING":
                    return ContractType.CurrentQuarter;
                case "NEXT_QUARTER":
                case "NEXT_QUARTER_DELIVERING":
                    return ContractType.NextQuarter;
                case "PERPETUAL_DELIVERING":
                case "PERPETUAL DELIVERING":
                    return ContractType.PerpetualDelivering;
                default:
                    throw new JsonException($"An unknown contract type \"{s}\" was encountered.");
            }
        }

        public static string Format(ContractStatus value)
        {
            switch (value)
            {
                case ContractStatus.PendingTrading:
                    return "PENDING_TRADING";
                case ContractStatus.Trading:
                    return "TRADING";
                case ContractStatus.PreDelivering:
                    return "PRE_DELIVERING";
                case ContractStatus.Delivering:
                    return "DELIVERING";
                case ContractStatus.Delivered:
                    return "DELIVERED";
                case ContractStatus.PreSettle:
                    return "PRE_SETTLE";
                case ContractStatus.Settling:
                    return "SETTLING";
                case ContractStatus.Close:
                    return "CLOSE";
                default:
                    throw new NotImplementedException($"The contract status \"{value}\" is not implemented.");
            }
        }

        public static ContractStatus ParseContractStatus(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The contract status value is null.");

            switch (s)
            {
                case "PENDING_TRADING":
                    return ContractStatus.PendingTrading;
                case "TRADING":
                    return ContractStatus.Trading;
                case "PRE_DELIVERING":
                    return ContractStatus.PreDelivering;
                case "DELIVERING":
                    return ContractStatus.Delivering;
                case "DELIVERED":
                    return ContractStatus.Delivered;
                case "PRE_SETTLE":
                    return ContractStatus.PreSettle;
                case "SETTLING":
                    return ContractStatus.Settling;
                case "CLOSE":
                    return ContractStatus.Close;
                default:
                    throw new JsonException($"An unknown contract status \"{s}\" was encountered.");
            }
        }

        public static string Format(OrderType value)
        {
            switch (value)
            {
                case OrderType.Limit:
                    return "LIMIT";
                case OrderType.Market:
                    return "MARKET";
                case OrderType.StopLossLimit:
                    return "STOP";
                case OrderType.StopLossMarket:
                    return "STOP_MARKET";
                case OrderType.TakeProfitLimit:
                    return "TAKE_PROFIT";
                case OrderType.TakeProfitMarket:
                    return "TAKE_PROFIT_MARKET";
                case OrderType.TrailingStopMarket:
                    return "TRAILING_STOP_MARKET";
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
                case "STOP":
                    return OrderType.StopLossLimit;
                case "STOP_MARKET":
                    return OrderType.StopLossMarket;
                case "TAKE_PROFIT":
                    return OrderType.TakeProfitLimit;
                case "TAKE_PROFIT_MARKET":
                    return OrderType.TakeProfitMarket;
                case "TRAILING_STOP_MARKET":
                    return OrderType.TrailingStopMarket;
                default:
                    throw new JsonException($"An unknown order type \"{s}\" was encountered.");
            }
        }

        public static string Format(TimeInForce value)
        {
            switch (value)
            {
                case TimeInForce.GoodTillCanceled:
                    return "GTC";
                case TimeInForce.FillOrKill:
                    return "FOK";
                case TimeInForce.ImmediateOrCancel:
                    return "IOC";
                case TimeInForce.GoodTillCrossing:
                    return "GTX";
                default:
                    throw new NotImplementedException($"The time-in-force type \"{value}\" is not implemented.");
            }
        }

        public static TimeInForce ParseTimeInForce(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The time in force rule value is null.");

            switch (s)
            {
                case "GTC":
                    return TimeInForce.GoodTillCanceled;
                case "IOC":
                    return TimeInForce.ImmediateOrCancel;
                case "FOK":
                    return TimeInForce.FillOrKill;
                case "GTX":
                    return TimeInForce.GoodTillCrossing;
                default:
                    throw new JsonException($"An unknown time in force rule \"{s}\" was encountered.");
            }
        }

        public static string Format(OrderResponseType value)
        {
            switch (value)
            {
                case OrderResponseType.Ack:
                    return "ACK";
                case OrderResponseType.Result:
                    return "RESULT";
                default:
                    throw new NotImplementedException($"The order response type \"{value}\" is not implemented.");
            }
        }

        public static OrderResponseType ParseOrderResponseType(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The order response type value is null.");

            switch (s)
            {
                case "ACK":
                    return OrderResponseType.Ack;
                case "RESULT":
                    return OrderResponseType.Result;
                default:
                    throw new JsonException($"An unknown order response type \"{s}\" was encountered.");
            }
        }

        public static string Format(KlineInterval value)
        {
            switch (value)
            {
                case KlineInterval.Minute1:
                    return "1m";
                case KlineInterval.Minute3:
                    return "3m";
                case KlineInterval.Minute5:
                    return "5m";
                case KlineInterval.Minute15:
                    return "15m";
                case KlineInterval.Minute30:
                    return "30m";
                case KlineInterval.Hour1:
                    return "1h";
                case KlineInterval.Hour2:
                    return "2h";
                case KlineInterval.Hour4:
                    return "4h";
                case KlineInterval.Hour6:
                    return "6h";
                case KlineInterval.Hour8:
                    return "8h";
                case KlineInterval.Hour12:
                    return "12h";
                case KlineInterval.Day1:
                    return "1d";
                case KlineInterval.Week1:
                    return "1w";
                case KlineInterval.Day3:
                    return "3d";
                case KlineInterval.Month1:
                    return "1M";
                default:
                    throw new NotImplementedException($"The kline interval \"{value}\" is not implemented.");
            }
        }

        public static string Format(StatsInterval value)
        {
            switch (value)
            {
                case StatsInterval.Minute5:
                    return "5m";
                case StatsInterval.Minute15:
                    return "15m";
                case StatsInterval.Minute30:
                    return "30m";
                case StatsInterval.Hour1:
                    return "1h";
                case StatsInterval.Hour2:
                    return "2h";
                case StatsInterval.Hour4:
                    return "4h";
                case StatsInterval.Hour6:
                    return "6h";
                case StatsInterval.Hour12:
                    return "12h";
                case StatsInterval.Day1:
                    return "1d";
                default:
                    throw new NotImplementedException($"The stats interval \"{value}\" is not implemented.");
            }
        }

        #endregion
    }
}
