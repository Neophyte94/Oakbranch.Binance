using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Oakbranch.Binance.Utility;
using Oakbranch.Binance.Models.Savings;
using Oakbranch.Binance.Core;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Clients
{
    public class SavingsApiClient : SapiClientBase
    {
        #region Constants

        // Constraints.
        /// <summary>
        /// Defines the maximum allowed period to query interest history within (in ticks).
        /// </summary>
        public const long MaxInterestLookupInterval = 30 * TimeSpan.TicksPerDay;

        // Endpoints.
        private const string GetFlexibleProductListEndpoint = "/sapi/v1/lending/daily/product/list";
        private const string GetLeftDailyPurchaseQuotaEndpoint = "/sapi/v1/lending/daily/userLeftQuota";
        private const string GetLeftDailyRedemptionQuotaEndpoint = "/sapi/v1/lending/daily/userRedemptionQuota";
        private const string PostPurchaseFlexibleProductEndpoint = "/sapi/v1/lending/daily/purchase";
        private const string PostRedeemFlexibleProductEndpoint = "/sapi/v1/lending/daily/redeem";
        private const string GetFlexibleProductPositionEndpoint = "/sapi/v1/lending/daily/token/position";
        private const string GetFixedProductListEndpoint = "/sapi/v1/lending/project/list";
        private const string PostPurchaseFixedProductEndpoint = "/sapi/v1/lending/project/list";
        private const string GetFixedProductPositionEndpoint = "/sapi/v1/lending/project/position/list";
        private const string GetSavingsAccountInfoEndpoint = "/sapi/v1/lending/union/account";
        private const string GetPurchaseRecordEndpoint = "/sapi/v1/lending/union/purchaseRecord";
        private const string GetRedemptionRecordEndpoint = "/sapi/v1/lending/union/redemptionRecord";
        private const string GetInterestHistoryEndpoint = "/sapi/v1/lending/union/interestHistory";
        private const string PostChangeFixedToFlexibleEndpoint = "/sapi/v1/lending/positionChanged";

        #endregion

        #region Instance constructors

        public SavingsApiClient(IApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger? logger)
            : base(connector, limitsRegistry, logger)
        { }

        #endregion

        #region Static methods

        private static string Format(SavingsProductType value)
        {
            return value switch
            {
                SavingsProductType.Flexible => "DAILY",
                SavingsProductType.Activity => "ACTIVITY",
                SavingsProductType.Fixed => "CUSTOMIZED_FIXED",
                _ => throw new NotImplementedException($"The savings product type \"{value}\" is not implemented."),
            };
        }

        private static SavingsProductType ParseLendingType(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new JsonException("The lending type value is null.");

            return s switch
            {
                "DAILY" => SavingsProductType.Flexible,
                "ACTIVITY" => SavingsProductType.Activity,
                "CUSTOMIZED_FIXED" => SavingsProductType.Fixed,
                _ => throw new JsonException($"An unknown lending type \"{s}\" was encountered."),
            };
        }

        #endregion

        #region Instance methods

        // Get account info.
        /// <summary>
        /// Prepares a query for information on the savings account.
        /// </summary>
        public IDeferredQuery<SavingsAccountInfo> PrepareGetAccountInfo()
        {
            ThrowIfNotRunning();

            string relEndpoint = GetSavingsAccountInfoEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 1),
            };

            return new DeferredQuery<SavingsAccountInfo>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, null, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseAccountInfo,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets a query for information on the savings account asynchronously.
        /// </summary>
        public Task<SavingsAccountInfo> GetAccountInfoAsync(CancellationToken ct)
        {
            using (IDeferredQuery<SavingsAccountInfo> query = PrepareGetAccountInfo())
            {
                return query.ExecuteAsync(ct);
            }
        }

        private SavingsAccountInfo ParseAccountInfo(byte[] data, object? _)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ParseUtility.ReadObjectStart(ref reader);
            SavingsAccountInfo sai = new SavingsAccountInfo();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                if (!reader.Read())
                {
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                }

                switch (propName)
                {
                    case "positionAmountVos":
                        sai.Positions = ParseAggrSavingsPositionList(ref reader);
                        break;
                    case "totalAmountInBTC":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out sai.TotalAmountInBTC);
                        break;
                    case "totalAmountInUSDT":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out sai.TotalAmountInUSDT);
                        break;
                    case "totalFixedAmountInBTC":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out sai.TotalFixedAmountInBTC);
                        break;
                    case "totalFixedAmountInUSDT":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out sai.TotalFixedAmountInUSDT);
                        break;
                    case "totalFlexibleInBTC":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out sai.TotalFlexibleAmountInBTC);
                        break;
                    case "totalFlexibleInUSDT":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out sai.TotalFlexibleAmountInUSDT);
                        break;
                    default:
                        LogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{propName}\" of the savings account info was encountered.");
                        reader.Skip();
                        break;
                }
            }

            return sai;
        }

        private List<AggregateSavingsPosition> ParseAggrSavingsPositionList(ref Utf8JsonReader reader)
        {
            ParseUtility.EnsureArrayStartToken(ref reader);
            List<AggregateSavingsPosition> resultList = new List<AggregateSavingsPosition>(32);

            string? asset = null;
            decimal amount = 0.0m;
            double amountInBtc = double.NaN, amountInUsdt = double.NaN;
            ParseSchemaValidator validator = new ParseSchemaValidator(4);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ParseUtility.EnsureObjectStartToken(ref reader);

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    ParseUtility.EnsurePropertyNameToken(ref reader);
                    string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                    if (!reader.Read())
                        throw ParseUtility.GenerateNoPropertyValueException(propName);
                    switch (propName)
                    {
                        case "amount":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out amount);
                            validator.RegisterProperty(0);
                            break;
                        case "amountInBTC":
                            ParseUtility.ParseDouble(propName, reader.GetString(), out amountInBtc);
                            validator.RegisterProperty(1);
                            break;
                        case "amountInUSDT":
                            ParseUtility.ParseDouble(propName, reader.GetString(), out amountInUsdt);
                            validator.RegisterProperty(2);
                            break;
                        case "asset":
                            asset = reader.GetString();
                            validator.RegisterProperty(3);
                            break;
                        default:
                            throw ParseUtility.GenerateUnknownPropertyException(propName);
                    }
                }

                if (!validator.IsComplete())
                {
                    const string objName = "aggregate savings position";
                    int missingPropNum = validator.GetMissingPropertyNumber();
                    throw missingPropNum switch
                    {
                        0 => ParseUtility.GenerateMissingPropertyException(objName, "amount"),
                        1 => ParseUtility.GenerateMissingPropertyException(objName, "amount in BTC"),
                        2 => ParseUtility.GenerateMissingPropertyException(objName, "amount in USDT"),
                        3 => ParseUtility.GenerateMissingPropertyException(objName, "asset"),
                        _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
                    };
                }

                resultList.Add(new AggregateSavingsPosition(asset!, amount, amountInBtc, amountInUsdt));
                validator.Reset();
            }

            return resultList;
        }

        // Get flexible product position.
        /// <summary>
        /// Prepares a query for active subscriptions on flexible savings products, either on all assets or only the specified one.
        /// </summary>
        /// <param name="asset">The asset to get subscriptions info on (optional).</param>
        public IDeferredQuery<List<FlexibleProductPosition>> PrepareGetFlexibleProductPositions(string? asset = null)
        {
            ThrowIfNotRunning();
            asset.ThrowIfEmptyOrWhitespace();

            string relEndpoint = GetFlexibleProductPositionEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 1),
            };

            QueryBuilder? qs = null;
            if (asset != null)
            {
                qs = new QueryBuilder(133);
                qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            }

            return new DeferredQuery<List<FlexibleProductPosition>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseFlexibleSavingsPosition,
                parseArgs: asset != null ? 2 : 32,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets active subscriptions on flexible savings products, either on all assets or only the specified one, asynchronously.
        /// </summary>
        public Task<List<FlexibleProductPosition>> GetFlexibleProductPositionsAsync(
            string? asset = null, CancellationToken ct = default)
        {
            using (IDeferredQuery<List<FlexibleProductPosition>> query = PrepareGetFlexibleProductPositions(asset))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private List<FlexibleProductPosition> ParseFlexibleSavingsPosition(byte[] data, object? parseArgs = null)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ParseUtility.ReadArrayStart(ref reader);
            List<FlexibleProductPosition> resultList = new List<FlexibleProductPosition>(
                parseArgs is int expectedCount ? expectedCount : 8);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ParseUtility.EnsureObjectStartToken(ref reader);

                FlexibleProductPosition pos = default;
                ParseSchemaValidator validator = new ParseSchemaValidator(13);


                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                    if (!reader.Read())
                    {
                        throw ParseUtility.GenerateNoPropertyValueException(propName);
                    }

                    switch (propName)
                    {
                        case "asset":
                            pos.Asset = ParseUtility.GetNonEmptyString(ref reader, propName);
                            validator.RegisterProperty(0);
                            break;
                        case "productId":
                            pos.ProductId = ParseUtility.GetNonEmptyString(ref reader, propName);
                            validator.RegisterProperty(1);
                            break;
                        case "productName":
                            pos.ProductName = ParseUtility.GetNonEmptyString(ref reader, propName);
                            validator.RegisterProperty(2);
                            break;
                        case "canRedeem":
                            pos.CanRedeem = reader.GetBoolean();
                            validator.RegisterProperty(3);
                            break;
                        case "totalAmount":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.TotalAmount);
                            validator.RegisterProperty(4);
                            break;
                        case "freeAmount":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.FreeAmount);
                            validator.RegisterProperty(5);
                            break;
                        case "redeemingAmount":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.RedeemingAmount);
                            validator.RegisterProperty(6);
                            break;
                        case "collateralAmount":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.CollateralAmount);
                            validator.RegisterProperty(7);
                            break;
                        case "totalInterest":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.TotalInterest);
                            validator.RegisterProperty(8);
                            break;
                        case "totalBonusRewards":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.TotalBonusRewards);
                            validator.RegisterProperty(9);
                            break;
                        case "totalMarketRewards":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.TotalMarketRewards);
                            validator.RegisterProperty(10);
                            break;
                        case "dailyInterestRate":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.DailyInterestRate);
                            validator.RegisterProperty(11);
                            break;
                        case "annualInterestRate":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.AnnualInterestRate);
                            validator.RegisterProperty(12);
                            break;
                        case "todayPurchasedAmount":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out pos.TodayPurchasedAmount);
                            break;
                        case "tierAnnualInterestRate":
                            pos.AnnualInterestRateTiers = ParseInterestRateTiers(ref reader);
                            break;
                        case "freezeAmount":
                        case "lockedAmount":
                            // These properties are obsolete and not stored.
                            break;
                        default:
                            LogMessage(LogLevel.Warning,
                                $"An unknown property \"{propName}\" of the flexible product position was encountered.");
                            reader.Skip();
                            break;
                    }
                }

                if (!validator.IsComplete())
                {
                    const string objName = "flexible product position";
                    int missingPropNum = validator.GetMissingPropertyNumber();
                    throw missingPropNum switch
                    {
                        0 => ParseUtility.GenerateMissingPropertyException(objName, "asset"),
                        1 => ParseUtility.GenerateMissingPropertyException(objName, "product ID"),
                        2 => ParseUtility.GenerateMissingPropertyException(objName, "product name"),
                        3 => ParseUtility.GenerateMissingPropertyException(objName, "can redeem"),
                        4 => ParseUtility.GenerateMissingPropertyException(objName, "total amount"),
                        5 => ParseUtility.GenerateMissingPropertyException(objName, "free amount"),
                        6 => ParseUtility.GenerateMissingPropertyException(objName, "redeeming amount"),
                        7 => ParseUtility.GenerateMissingPropertyException(objName, "collateral amount"),
                        8 => ParseUtility.GenerateMissingPropertyException(objName, "total earned interest"),
                        9 => ParseUtility.GenerateMissingPropertyException(objName, "total bonus rewards"),
                        10 => ParseUtility.GenerateMissingPropertyException(objName, "total market rewards"),
                        11 => ParseUtility.GenerateMissingPropertyException(objName, "daily interest rate"),
                        12 => ParseUtility.GenerateMissingPropertyException(objName, "annual interest rate"),
                        _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
                    };
                }

                resultList.Add(pos);
                validator.Reset();
            }

            return resultList;
        }

        private List<InterestRateTier> ParseInterestRateTiers(ref Utf8JsonReader reader)
        {
            ParseUtility.EnsureObjectStartToken(ref reader);
            List<InterestRateTier> result = new List<InterestRateTier>(3);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string tierName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                if (!reader.Read())
                {
                    throw new JsonException(
                        $"A value for the interest rate tier \"{tierName}\" " +
                        $"was expected but \"{reader.TokenType}\" encountered.");
                }

                ParseUtility.ParseDouble(tierName, reader.GetString(), out double tierValue);
                result.Add(new InterestRateTier(tierName, tierValue));
            }

            return result;
        }

        // Get interest history.
        /// <summary>
        /// Prepares a query for the interest (distribution) history for the specified savings product or products type.
        /// </summary>
        /// <param name="type">The type of the savings product to get interest history for.</param>
        /// <param name="asset">
        /// The asset to to get interest history for (optional).
        /// <para>If not specified, the data on all savings products of the specified <paramref name="type"/> will be returned.</para>
        /// </param>
        /// <param name="startTime">
        /// Date &amp; time to get records from (optional).
        /// <para>If <paramref name="endTime"/> is also specified, the time between the two values mustn't be longer than 30 days.</para>
        /// <para>If <paramref name="startTime"/> and <paramref name="endTime"/> are both not sent the last 30 days' data will be returned.</para>
        /// </param>
        /// <param name="endTime">
        /// Date &amp; time to get records prior to (optional).
        /// <para>If <paramref name="startTime"/> is also specified, the time between the two values mustn't be longer than 30 days.</para>
        /// <para>If <paramref name="startTime"/> and <paramref name="endTime"/> are both not sent the last 30 days' data will be returned.</para>
        /// </param>
        /// <param name="currentPage">
        /// A results page to query, starting from 1 (optional).
        /// <para>If not specified, the default value 1 will be used.</para>
        /// </param>
        /// <param name="pageSize">
        /// A limit of records per page (optional).
        /// <para>The default value is 10. The maximum value is 100.</para>
        /// </param>
        public IDeferredQuery<List<InterestRecord>> PrepareGetInterestHistory(
            SavingsProductType type,
            string? asset = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int? currentPage = null,
            int? pageSize = null)
        {
            ThrowIfNotRunning();
            asset.ThrowIfEmptyOrWhitespace();
            if (startTime != null && endTime != null)
            {
                TimeSpan diff = endTime.Value - startTime.Value;
                if (diff.Ticks < 0)
                    throw new ArgumentException($"The specifed time period [{startTime} ; {endTime}] is invalid.");
                else if (diff.Ticks > MaxInterestLookupInterval)
                    throw new ArgumentException(
                        $"The duration of the specified time period [{startTime} ; {endTime}] exceeds the the limit " +
                        $"({new TimeSpan(MaxInterestLookupInterval).TotalDays} days). Check {nameof(MaxInterestLookupInterval)}.");
            }
            if (currentPage < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(currentPage));
            }
            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            string relEndpoint = GetInterestHistoryEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 1),
            };

            QueryBuilder qs = new QueryBuilder(229);
            qs.AddParameter("lendingType", Format(type));
            if (asset != null)
            {
                qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            }
            if (startTime != null)
            {
                qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
            }
            if (endTime != null)
            {
                qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
            }
            if (currentPage != null)
            {
                qs.AddParameter("current", currentPage.Value);
            }
            if (pageSize != null)
            {
                qs.AddParameter("size", pageSize.Value);
            }

            return new DeferredQuery<List<InterestRecord>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseSavingsInterestRecords,
                parseArgs: pageSize != null ? pageSize.Value : null,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets the interest (distribution) history for the specified savings product or products type, asynchronously.
        /// </summary>
        public Task<List<InterestRecord>> GetInterestHistoryAsync(
            SavingsProductType type,
            string? asset = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int? currentPage = null,
            int? pageSize = null,
            CancellationToken ct = default)
        {
            using (IDeferredQuery<List<InterestRecord>> query = PrepareGetInterestHistory(
                type, asset, startTime, endTime, currentPage, pageSize))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private List<InterestRecord> ParseSavingsInterestRecords(byte[] data, object? parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ParseUtility.ReadArrayStart(ref reader);
            List<InterestRecord> resultList = new List<InterestRecord>(
                parseArgs is int expectedCount ? expectedCount : 10);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ParseUtility.EnsureObjectStartToken(ref reader);

                string? asset = null, productName = null;
                decimal interest = 0.0m;
                DateTime time = DateTime.MinValue;
                SavingsProductType productType = default;
                ParseSchemaValidator validator = new ParseSchemaValidator(4);

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string propName = ParseUtility.GetNonEmptyPropertyName(ref reader);

                    if (!reader.Read())
                        throw ParseUtility.GenerateNoPropertyValueException(propName);
                    switch (propName)
                    {
                        case "asset":
                            asset = reader.GetString();
                            validator.RegisterProperty(0);
                            break;
                        case "interest":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out interest);
                            validator.RegisterProperty(1);
                            break;
                        case "lendingType":
                            productType = ParseLendingType(ParseUtility.GetNonEmptyString(ref reader, propName));
                            validator.RegisterProperty(2);
                            break;
                        case "productName":
                            productName = reader.GetString();
                            break;
                        case "time":
                            time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                            validator.RegisterProperty(3);
                            break;
                        default:
                            LogMessage(LogLevel.Warning, $"An unknown interest record property \"{propName}\" was encountered.");
                            reader.Skip();
                            break;
                    }
                }

                if (!validator.IsComplete())
                {
                    const string objName = "savings interest record";
                    int missingPropNum = validator.GetMissingPropertyNumber();
                    throw missingPropNum switch
                    {
                        0 => ParseUtility.GenerateMissingPropertyException(objName, "asset"),
                        1 => ParseUtility.GenerateMissingPropertyException(objName, "interest"),
                        2 => ParseUtility.GenerateMissingPropertyException(objName, "product type"),
                        3 => ParseUtility.GenerateMissingPropertyException(objName, "time"),
                        _ => ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
                    };
                }

                resultList.Add(new InterestRecord(asset!, interest, time, productType, productName));
                validator.Reset();
            }

            return resultList;
        }

        #endregion
    }
}
