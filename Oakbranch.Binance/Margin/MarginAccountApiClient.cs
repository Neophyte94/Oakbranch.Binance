using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Oakbranch.Binance.RateLimits;
using Oakbranch.Common.Logging;

namespace Oakbranch.Binance.Margin
{
    public class MarginAccountApiClient : SapiClientBase
    {
        #region Constants

        // Common.
        /// <summary>
        /// Defines the expected number of cross margin trading pairs reported by the server.
        /// <para>There were 400 cross margin pairs as of 09.06.2022.</para>
        /// </summary>
        public const int ExpectedCrossPairsCount = 500;
        /// <summary>
        /// Defines the expected number of isolated margin trading pairs reported by the server.
        /// <para>There were 813 isolated margin pairs as of 09.06.2022.</para>
        /// </summary>
        public const int ExpectedIsolatedPairsCount = 1000;

        // Constraints.
        /// <summary>
        /// Defines the maximum allowed number of enabled isolated margin accounts.
        /// </summary>
        public const int MaxIsolatedAccountCount = 10;
        /// <summary>
        /// Defines the maximum item capacity of a single page of query results.
        /// </summary>
        public const int MaxResultPageSize = 100;
        /// <summary>
        /// Defines the default item capacity of a single page of query results.
        /// </summary>
        public const int DefaultResultPageSize = 10;
        /// <summary>
        /// Defines the maximum allowed period to query asset transactions history within (in ticks).
        /// </summary>
        public const long MaxTransactionLookupInterval = 30 * TimeSpan.TicksPerDay;
        /// <summary>
        /// Defines the maximum allowed period to query account orders within (in ticks).
        /// </summary>
        public const long MaxAccountOrderLookupInterval = 24 * TimeSpan.TicksPerHour;
        /// <summary>
        /// Defines the maximum allowed period to query account trades within (in ticks).
        /// </summary>
        public const long MaxAccountTradeLookupInterval = 24 * TimeSpan.TicksPerHour;

        // Endpoints.
        private const string GetCrossAccountInfoEndpoint = "/sapi/v1/margin/account";
        private const string GetIsolatedAccountInfoEndpoint = "/sapi/v1/margin/isolated/account";
        private const string GetEnabledIsolatedAccountsLimitEndpoint = "/sapi/v1/margin/isolated/accountLimit";
        private const string PostEnableIsolatedAccountEndpoint = "/sapi/v1/margin/isolated/account";
        private const string DeleteDisableIsolatedAccountEndpoint = "/sapi/v1/margin/isolated/account";
        private const string PostCrossTransferEndpoint = "/sapi/v1/margin/transfer";
        private const string PostIsolatedTransferEndpoint = "/sapi/v1/margin/isolated/transfer";
        private const string GetCrossTransferHistoryEndpoint = "/sapi/v1/margin/transfer";
        private const string GetIsolatedTransferHistoryEndpoint = "/sapi/v1/margin/isolated/transfer";
        private const string PostBorrowEndpoint = "/sapi/v1/margin/loan";
        private const string PostRepayEndpoint = "/sapi/v1/margin/repay";
        private const string GetBorrowRecordEndpoint = "/sapi/v1/margin/loan";
        private const string GetRepayRecordEndpoint = "/sapi/v1/margin/repay";
        private const string GetInterestHistoryEndpoint = "/sapi/v1/margin/interestHistory";
        private const string GetForceLiquidationRecordEndpoint = "/sapi/v1/margin/forceLiquidationRec";
        private const string GetMaxBorrowEndpoint = "/sapi/v1/margin/maxBorrowable";
        private const string GetMaxTransferOutEndpoint = "/sapi/v1/margin/maxTransferable";
        private const string GetMarginSummaryEndpoint = "/sapi/v1/margin/tradeCoeff";
        private const string GetAssetEndpoint = "/sapi/v1/margin/asset";
        private const string GetAllAssetsEndpoint = "/sapi/v1/margin/allAssets";
        private const string GetCrossPairEndpoint = "/sapi/v1/margin/pair";
        private const string GetAllCrossPairsEndpoint = "/sapi/v1/margin/allPairs";
        private const string GetIsolatedSymbolEndpoint = "/sapi/v1/margin/isolated/pair";
        private const string GetAllIsolatedSymbolsEndpoint = "/sapi/v1/margin/isolated/allPairs";
        private const string GetPriceIndexEndpoint = "/sapi/v1/margin/priceIndex";
        private const string PostNewOrderEndpoint = "/sapi/v1/margin/order";
        private const string DeleteOrderEndpoint = "/sapi/v1/margin/order";
        private const string DeleteAllOpenOrdersOnSymbolEndpoint = "/sapi/v1/margin/openOrders";
        private const string GetOrderEndpoint = "/sapi/v1/margin/order";
        private const string GetOpenOrdersEndpoint = "/sapi/v1/margin/openOrders";
        private const string GetAllOrdersEndpoint = "/sapi/v1/margin/allOrders";
        private const string PostNewOCOEndpoint = "/sapi/v1/margin/order/oco";
        private const string DeleteOCOEndpoint = "/sapi/v1/margin/orderList";
        private const string GetOCOsEndpoint = "/sapi/v1/margin/orderList";
        private const string GetOpenOCOsEndpoint = "/sapi/v1/margin/openOrderList";
        private const string GetAllOCOsEndpoint = "/sapi/v1/margin/allOrderList";
        private const string GetAccountTradeListEndpoint = "/sapi/v1/margin/myTrades";
        private const string PostToggleBNBBurnEndpoint = "/sapi/v1/bnbBurn";
        private const string GetBNBBurnStatusEndpoint = "/sapi/v1/bnbBurn";
        private const string GetInterestRateHistoryEndpoint = "/sapi/v1/margin/interestRateHistory";
        private const string GetFutureInterestRateEndpoint = "/sapi/v1/margin/next-hourly-interest-rate";
        private const string GetCrossFeeDataEndpoint = "/sapi/v1/margin/crossMarginData";
        private const string GetIsolatedFeeDataEndpoint = "/sapi/v1/margin/isolatedMarginData";
        private const string GetIsolatedTierDataEndpoint = "/sapi/v1/margin/isolatedMarginTier";
        private const string GetOrderLimitUsageEndpoint = "/sapi/v1/margin/rateLimit/order";
        private const string GetDustlogEndpoint = "/sapi/v1/margin/dribblet";
        private const string GetCrossCollateralRatioEndpoint = "/sapi/v1/margin/crossMarginCollateralRatio";
        private const string GetSmallLiabilityExchangeCoinsEndpoint = "/sapi/v1/margin/exchange-small-liability";
        private const string PostSmallLiabilityExchangeEndpoint = "/sapi/v1/margin/exchange-small-liability";
        private const string GetSmallLiabilityExchangeHistoryEndpoint = "/sapi/v1/margin/exchange-small-liability-history";

        #endregion

        #region Instance members

        protected override string LogContextName => "Binance MA API client";

        #endregion

        #region Instance constructors

        public MarginAccountApiClient(IApiConnector connector, IRateLimitsRegistry limitsRegistry, ILogger logger) :
            base(connector, limitsRegistry, logger)
        {

        }

        #endregion

        #region Static methods

        private static string Format(TransferDirection value)
        {
            if (value == TransferDirection.RollIn)
                return "ROLL_IN";
            else if (value == TransferDirection.RollOut)
                return "ROLL_OUT";
            else
                throw new NotImplementedException($"The transfer direction \"{value}\" is not implemented.");
        }

        private static TransferDirection ParseTransferDirection(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The transfer direction value is null.");

            switch (s)
            {
                case "ROLL_IN":
                    return TransferDirection.RollIn;
                case "ROLL_OUT":
                    return TransferDirection.RollOut;
                default:
                    throw new JsonException($"The transfer direction \"{s}\" is unknown.");
            }
        }

        private static string Format(AccountType value)
        {
            switch (value)
            {
                case AccountType.Spot:
                    return "SPOT";
                case AccountType.IsolatedMargin:
                    return "ISOLATED_MARGIN";
                default:
                    throw new NotSupportedException($"The account type \"{value}\" is not supported.");
            }
        }

        private static AccountType ParseAccountType(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The specified account type value is null.");

            switch (s)
            {
                case "SPOT":
                    return AccountType.Spot;
                case "ISOLATED_MARGIN":
                    return AccountType.IsolatedMargin;
                default:
                    throw new JsonException($"The unknown account type \"{s}\" was encountered.");
            }
        }

        private static string Format(MarginSideEffect value)
        {
            switch (value)
            {
                case MarginSideEffect.NoSideEffect:
                    return "NO_SIDE_EFFECT";
                case MarginSideEffect.MarginBuy:
                    return "MARGIN_BUY";
                case MarginSideEffect.AutoRepay:
                    return "AUTO_REPAY";
                default:
                    throw new NotImplementedException($"The margin side effect type \"{value}\" is not implemented.");
            }
        }

        private static MarginStatus ParseMarginStatus(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The margin status value is null.");

            switch (s)
            {
                case "EXCESSIVE":
                    return MarginStatus.Excessive;
                case "NORMAL":
                    return MarginStatus.Normal;
                case "MARGIN_CALL":
                    return MarginStatus.MarginCall;
                case "PRE_LIQUIDATION":
                    return MarginStatus.PreLiquidation;
                case "FORCE_LIQUIDATION":
                    return MarginStatus.ForceLiquidation;
                default:
                    throw new JsonException($"An unknown margin status \"{s}\" was encountered.");
            }
        }

        private static TransactionStatus ParseTransactionStatus(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The transaction status value is null.");

            switch (s)
            {
                case "PENDING":
                    return TransactionStatus.Pending;
                case "CONFIRMED":
                case "CONFIRM":
                    return TransactionStatus.Confirmed;
                case "FAILED":
                    return TransactionStatus.Failed;
                default:
                    throw new JsonException($"An unknown transaction status \"{s}\" was encountered.");
            }
        }

        private static string Format(TimeInForce value)
        {
            switch (value)
            {
                case TimeInForce.GoodTillCanceled:
                    return "GTC";
                case TimeInForce.FillOrKill:
                    return "FOK";
                case TimeInForce.ImmediateOrCancel:
                    return "IOC";
                default:
                    throw new NotImplementedException($"The time-in-force type \"{value}\" is not implemented.");
            }
        }

        private static TimeInForce ParseTimeInForce(string s)
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
                default:
                    throw new JsonException($"An unknown time in force rule \"{s}\" was encountered.");
            }
        }

        private static string Format(OrderType value)
        {
            switch (value)
            {
                case OrderType.Limit:
                    return "LIMIT";
                case OrderType.LimitMaker:
                    return "LIMIT_MAKER";
                case OrderType.Market:
                    return "MARKET";
                case OrderType.StopLoss:
                    return "STOP_LOSS";
                case OrderType.StopLossLimit:
                    return "STOP_LOSS_LIMIT";
                case OrderType.TakeProfit:
                    return "TAKE_PROFIT";
                case OrderType.TakeProfitLimit:
                    return "TAKE_PROFIT_LIMIT";
                default:
                    throw new NotImplementedException($"The order type \"{value}\" is not implemented.");
            }
        }

        private static OrderType ParseOrderType(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The order type value is null.");

            switch (s)
            {
                case "LIMIT":
                    return OrderType.Limit;
                case "LIMIT_MAKER":
                    return OrderType.LimitMaker;
                case "MARKET":
                    return OrderType.Market;
                case "STOP_LOSS":
                    return OrderType.StopLoss;
                case "STOP_LOSS_LIMIT":
                    return OrderType.StopLossLimit;
                case "TAKE_PROFIT":
                    return OrderType.TakeProfit;
                case "TAKE_PROFIT_LIMIT":
                    return OrderType.TakeProfitLimit;
                default:
                    throw new JsonException($"The order type \"{s}\" is unknown.");
            }
        }

        private static string Format(OrderResponseType value)
        {
            switch (value)
            {
                case OrderResponseType.Ack:
                    return "ACK";
                case OrderResponseType.Full:
                    return "FULL";
                case OrderResponseType.Result:
                    return "RESULT";
                default:
                    throw new NotImplementedException($"The order response type \"{value}\" is not implemented.");
            }
        }

        #endregion

        #region Instance methods

        // All cross margin pairs.
        /// <summary>
        /// Prepares a query for all available cross margin trading pairs.
        /// </summary>
        public IDeferredQuery<List<MarginPair>> PrepareGetCrossPairs()
        {
            ThrowIfNotRunning();

            string relEndpoint = GetAllCrossPairsEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 1),
            };

            return new DeferredQuery<List<MarginPair>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, null, false),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseTradingPairList,
                parseArgs: false,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets all available cross margin trading pairs asynchronously.
        /// </summary>
        public Task<List<MarginPair>> GetCrossPairsAsync(CancellationToken ct = default)
        {
            using (IDeferredQuery<List<MarginPair>> query = PrepareGetCrossPairs())
            {
                return query.ExecuteAsync(ct);
            }
        }

        // All isolated margin pairs.
        /// <summary>
        /// Prepares a query for all available isolated margin trading pairs.
        /// </summary>
        public IDeferredQuery<List<MarginPair>> PrepareGetIsolatedPairs()
        {
            ThrowIfNotRunning();

            string relEndpoint = GetAllIsolatedSymbolsEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            return new DeferredQuery<List<MarginPair>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, null, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseTradingPairList,
                parseArgs: true,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets all available isolated margin trading pairs asynchronously.
        /// </summary>
        public Task<List<MarginPair>> GetIsolatedPairsAsync(CancellationToken ct = default)
        {
            using (IDeferredQuery<List<MarginPair>> query = PrepareGetIsolatedPairs())
            {
                return query.ExecuteAsync(ct);
            }
        }

        // Cross margin account info.
        /// <summary>
        /// Prepares a query for information on the current state of the cross margin account.
        /// </summary>
        public IDeferredQuery<CrossAccountInfo> PrepareGetCrossAccountInfo()
        {
            ThrowIfNotRunning();

            string relEndpoint = GetCrossAccountInfoEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            return new DeferredQuery<CrossAccountInfo>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, null, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseCrossAccountInfo,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets information on the current state of the cross margin account asynchronously.
        /// </summary>
        public Task<CrossAccountInfo> GetCrossAccountInfoAsync(CancellationToken ct = default)
        {
            using (IDeferredQuery<CrossAccountInfo> query = PrepareGetCrossAccountInfo())
            {
                return query.ExecuteAsync(ct);
            }
        }

        private CrossAccountInfo ParseCrossAccountInfo(byte[] data, object parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ParseUtility.ReadObjectStart(ref reader);
            CrossAccountInfo rsp = new CrossAccountInfo();
            ParseSchemaValidator validator = new ParseSchemaValidator(8);

            // Parse the properties.
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                switch (propName)
                {
                    case "borrowEnabled":
                        rsp.IsBorrowEnabled = reader.GetBoolean();
                        validator.RegisterProperty(0);
                        break;
                    case "marginLevel":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out rsp.MarginLevel);
                        validator.RegisterProperty(1);
                        break;
                    case "totalAssetOfBtc":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out rsp.TotalAssetOfBTC);
                        validator.RegisterProperty(2);
                        break;
                    case "totalLiabilityOfBtc":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out rsp.TotalLiabilityOfBTC);
                        validator.RegisterProperty(3);
                        break;
                    case "totalNetAssetOfBtc":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out rsp.TotalNetAssetOfBTC);
                        validator.RegisterProperty(4);
                        break;
                    case "tradeEnabled":
                        rsp.IsTradeEnabled = reader.GetBoolean();
                        validator.RegisterProperty(5);
                        break;
                    case "transferEnabled":
                        rsp.IsTransferEnabled = reader.GetBoolean();
                        validator.RegisterProperty(6);
                        break;
                    case "userAssets":
                        rsp.UserAssets = ParseCrossAssetList(ref reader, 8);
                        validator.RegisterProperty(7);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "cross margin acc info";
                int missingPropNum = validator.GetMissingPropertyNumber();
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "borrow enabled");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "margin level");
                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "total asset of BTC");
                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "total liability of BTC");
                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "total net asset of BTC");
                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "trade enabled");
                    case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "transfer enabled");
                    case 7: throw ParseUtility.GenerateMissingPropertyException(objName, "user assets");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            // Return the result.
            return rsp;
        }

        private List<CrossAsset> ParseCrossAssetList(ref Utf8JsonReader reader, int expectedCount)
        {
            ParseUtility.ValidateArrayStartToken(ref reader);
            List<CrossAsset> resultList = new List<CrossAsset>(expectedCount);

            string asset = null;
            decimal free = 0.0m, borrowed = 0.0m, interest = 0.0m, locked = 0.0m, netAsset = 0.0m;
            ParseSchemaValidator validator = new ParseSchemaValidator(6);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ParseUtility.ValidateObjectStartToken(ref reader);

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    ParseUtility.ValidatePropertyNameToken(ref reader);
                    string propName = reader.GetString();

                    if (!reader.Read())
                        throw ParseUtility.GenerateNoPropertyValueException(propName);
                    switch (propName)
                    {
                        case "asset":
                            asset = reader.GetString();
                            validator.RegisterProperty(0);
                            break;
                        case "borrowed":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out borrowed);
                            validator.RegisterProperty(1);
                            break;
                        case "free":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out free);
                            validator.RegisterProperty(2);
                            break;
                        case "interest":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out interest);
                            validator.RegisterProperty(3);
                            break;
                        case "locked":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out locked);
                            validator.RegisterProperty(4);
                            break;
                        case "netAsset":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out netAsset);
                            validator.RegisterProperty(5);
                            break;
                        default:
                            throw ParseUtility.GenerateUnknownPropertyException(propName);
                    }
                }

                if (!validator.IsComplete())
                {
                    const string objName = "cross margin asset";
                    int missingPropNum = validator.GetMissingPropertyNumber();
                    switch (missingPropNum)
                    {
                        case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "asset symbol");
                        case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "borrowed");
                        case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "free");
                        case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "interest");
                        case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "locked");
                        case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "net asset");
                        default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                    }
                }

                resultList.Add(new CrossAsset(asset, borrowed, free, interest, locked, netAsset));
                validator.Reset();
            }

            return resultList;
        }

        // Isolated margin account info.
        /// <summary>
        /// Prepares a query for info on isolated margin accounts, either all or the specified ones only.
        /// </summary>
        /// <param name="symbols">
        /// Isolated margin symbols to get account info for (maximum 5 symbols).
        /// <para>Use <see langword="null"/> to get info on all pairs.</para>
        /// </param>
        public IDeferredQuery<IsolatedAccountsInfo> PrepareGetIsolatedAccountsInfo(params string[] symbols)
        {
            ThrowIfNotRunning();

            if (symbols != null && symbols.Length != 0)
            {
                for (int i = 0; i != symbols.Length; ++i)
                {
                    string smb = symbols[i];
                    if (String.IsNullOrWhiteSpace(smb))
                    {
                        throw new ArgumentException(
                            $"The specified symbols array contains a null or empty string at the index {i}.");
                    }
                    symbols[i] = CommonUtility.NormalizeSymbol(smb);
                }
            }

            string relEndpoint = GetIsolatedAccountInfoEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            QueryBuilder qs;
            if (symbols == null || symbols.Length == 0)
            {
                qs = new QueryBuilder(121);
            }
            else
            {
                qs = new QueryBuilder(173);
                qs.AddParameter("symbols", symbols);
            }

            return new DeferredQuery<IsolatedAccountsInfo>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, null, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseIsolatedAccountInfoResponse,
                parseArgs: symbols != null ? symbols.Length : MaxIsolatedAccountCount,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets info on isolated margin accounts asynchronously, either all or the specified ones only.
        /// </summary>
        public Task<IsolatedAccountsInfo> GetIsolatedAccountsInfoAsync(string[] symbols = null, CancellationToken ct = default)
        {
            using (IDeferredQuery<IsolatedAccountsInfo> query = PrepareGetIsolatedAccountsInfo(symbols))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private IsolatedAccountsInfo ParseIsolatedAccountInfoResponse(byte[] data, object parseArgs)
        {
            int expectedAccCount = parseArgs is int eac ? eac : MaxIsolatedAccountCount;
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ParseUtility.ReadObjectStart(ref reader);
            IsolatedAccountsInfo rsp = new IsolatedAccountsInfo();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                switch (propName)
                {
                    case "assets":
                        rsp.IsolatedSymbols = ParseIsolatedSymbolAccList(ref reader, expectedAccCount);
                        break;
                    case "totalAssetOfBtc":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out double ta);
                        rsp.TotalAssetOfBTC = ta;
                        break;
                    case "totalLiabilityOfBtc":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out double tl);
                        rsp.TotalLiabilityOfBTC = tl;
                        break;
                    case "totalNetAssetOfBtc":
                        ParseUtility.ParseDouble(propName, reader.GetString(), out double tna);
                        rsp.TotalNetAssetOfBTC = tna;
                        break;
                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{propName}\" of the isolated margin accounts summary was encountered.");
                        reader.Skip();
                        break;
                }
            }

            if (rsp.IsolatedSymbols == null)
            {
                const string objName = "isolated accounts summary";
                throw ParseUtility.GenerateMissingPropertyException(objName, "isolated symbol accounts");
            }

            return rsp;
        }

        private List<IsolatedSymbolAccInfo> ParseIsolatedSymbolAccList(ref Utf8JsonReader reader, int expectedCount)
        {
            ParseUtility.ValidateArrayStartToken(ref reader);
            List<IsolatedSymbolAccInfo> resultList = new List<IsolatedSymbolAccInfo>(expectedCount);
            ParseSchemaValidator validator = new ParseSchemaValidator(11);

            // Parse each isolated symbol account object.
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ParseUtility.ValidateObjectStartToken(ref reader);
                IsolatedSymbolAccInfo info = new IsolatedSymbolAccInfo();

                // Parse all the properties of the isolated symbol account object.
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    ParseUtility.ValidatePropertyNameToken(ref reader);
                    string propName = reader.GetString();

                    if (!reader.Read())
                        throw ParseUtility.GenerateNoPropertyValueException(propName);
                    switch (propName)
                    {
                        case "baseAsset":
                            info.BaseAsset = ParseIsolatedAsset(ref reader);
                            validator.RegisterProperty(0);
                            break;
                        case "quoteAsset":
                            info.QuoteAsset = ParseIsolatedAsset(ref reader);
                            validator.RegisterProperty(1);
                            break;
                        case "symbol":
                            info.Symbol = reader.GetString();
                            validator.RegisterProperty(2);
                            break;
                        case "isolatedCreated":
                            info.IsCreated = reader.GetBoolean();
                            validator.RegisterProperty(3);
                            break;
                        case "enabled":
                            info.IsEnabled = reader.GetBoolean();
                            validator.RegisterProperty(4);
                            break;
                        case "marginLevel":
                            ParseUtility.ParseDouble(propName, reader.GetString(), out info.MarginLevel);
                            validator.RegisterProperty(5);
                            break;
                        case "marginLevelStatus":
                            info.MarginLevelStatus = ParseMarginStatus(reader.GetString());
                            break;
                        case "marginRatio":
                            ParseUtility.ParseDouble(propName, reader.GetString(), out info.MarginRatio);
                            validator.RegisterProperty(6);
                            break;
                        case "indexPrice":
                            ParseUtility.ParseDouble(propName, reader.GetString(), out info.IndexPrice);
                            validator.RegisterProperty(7);
                            break;
                        case "liquidatePrice":
                            ParseUtility.ParseDouble(propName, reader.GetString(), out info.LiquidatePrice);
                            validator.RegisterProperty(8);
                            break;
                        case "liquidateRate":
                            ParseUtility.ParseDouble(propName, reader.GetString(), out info.LiquidateRate);
                            validator.RegisterProperty(9);
                            break;
                        case "tradeEnabled":
                            info.IsTradeEnabled = reader.GetBoolean();
                            validator.RegisterProperty(10);
                            break;
                        default:
                            PostLogMessage(LogLevel.Warning, $"An unknown isolated symbol acc property \"{propName}\" was encountered.");
                            reader.Skip();
                            break;
                    }
                }

                // Validate whether all the essential properties were provided.
                if (!validator.IsComplete())
                {
                    const string objName = "isolated margin symbol acc";
                    int missingPropNum = validator.GetMissingPropertyNumber();
                    switch (missingPropNum)
                    {
                        case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "base asset");
                        case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "quote asset");
                        case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "symbol");
                        case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "is created");
                        case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "is enabled");
                        case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "margin level");
                        case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "margin ratio");
                        case 7: throw ParseUtility.GenerateMissingPropertyException(objName, "index price");
                        case 8: throw ParseUtility.GenerateMissingPropertyException(objName, "liquidation price");
                        case 9: throw ParseUtility.GenerateMissingPropertyException(objName, "liquidation rate");
                        case 10: throw ParseUtility.GenerateMissingPropertyException(objName, "is trade enabled");
                        default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                    }
                }

                // Add the instance to the result list.
                resultList.Add(info);
                validator.Reset();
            }

            return resultList;
        }

        private IsolatedAsset ParseIsolatedAsset(ref Utf8JsonReader reader)
        {
            ParseUtility.ValidateObjectStartToken(ref reader);
            string asset = null;
            bool isBorrowEnabled = default, isRepayEnabled = default;
            decimal borrowed = 0.0m, free = 0.0m, interest = 0.0m, locked = 0.0m;
            decimal netAsset = 0.0m, netAssetOfBtc = 0.0m, totalAsset = 0.0m;
            ParseSchemaValidator validator = new ParseSchemaValidator(7);

            // Parse all the properties.
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                switch (propName)
                {
                    case "asset":
                        asset = reader.GetString();
                        validator.RegisterProperty(0);
                        break;
                    case "borrowEnabled":
                        isBorrowEnabled = reader.GetBoolean();
                        validator.RegisterProperty(1);
                        break;
                    case "borrowed":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out borrowed);
                        validator.RegisterProperty(2);
                        break;
                    case "free":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out free);
                        validator.RegisterProperty(3);
                        break;
                    case "interest":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out interest);
                        validator.RegisterProperty(4);
                        break;
                    case "locked":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out locked);
                        validator.RegisterProperty(5);
                        break;
                    case "netAsset":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out netAsset);
                        break;
                    case "netAssetOfBtc":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out netAssetOfBtc);
                        break;
                    case "repayEnabled":
                        isRepayEnabled = reader.GetBoolean();
                        validator.RegisterProperty(6);
                        break;
                    case "totalAsset":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out totalAsset);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            // Validate whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "isolated margin asset";
                int missingPropNum = validator.GetMissingPropertyNumber();
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "asset symbol");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "is borrow enabled");
                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "borrowed");
                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "free");
                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "interest");
                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "locked");
                    case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "is repay enabled");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            // Return the result.
            return new IsolatedAsset(
                asset, isBorrowEnabled, isRepayEnabled, borrowed, free, interest, locked,
                netAsset, netAssetOfBtc, totalAsset);
        }

        // Margin transfer history.
        /// <summary>
        /// Prepares a query for cross margin transfer history with the specified parameters.
        /// <para>The records are returned ordered by time from newer to older.</para>
        /// </summary>
        /// <param name="crossAsset">An asset to request transactions for. 
        /// <para>If not specified, transactions for all assets are returned.</para></param>
        /// <param name="direction">The direction of trasnfer transaction to fetch.
        /// <para>If not specified, transactions of all directions will be returned.</para>
        /// </param>
        /// <param name="startTime">
        /// A historical date and time to request transactions from. 
        /// <para>If not specified, the latest transactions before the <paramref name="endTime"/> or the current time will be returned.</para>
        /// </param>
        /// <param name="endTime">A historical date and time to request transactions prior.
        /// <para>If not specified, transactions prior the current time will be returned.</para></param>
        /// <param name="currentPage">A number of the lookup page in the results list to return (starting from 1).
        /// <para>If not specified, the 1st page will be returned.</para></param>
        /// <param name="pageSize">A limit of transactions per page to return.
        /// <para>If not specified, the default limit (10) will be used. The maximum value is <see cref="MaxResultPageSize"/> (100).</para></param>
        /// <param name="isArchived">Specifies whether to look for archived data from 6 months ago.
        /// <para>If not specified, <see langword="false"/> will be used.</para></param>
        public IDeferredQuery<ResultsPage<TransferTransaction>> PrepareGetCrossTrasferHistory(
            string crossAsset = null, TransferDirection? direction = null,
            DateTime? startTime = null, DateTime? endTime = null,
            int? currentPage = null, byte? pageSize = null, bool? isArchived = null)
        {
            ThrowIfNotRunning();
            ValidateTransactionHistoryInterval(ref startTime, ref endTime);
            if (pageSize < 1 || pageSize > MaxResultPageSize)
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            if (currentPage < 1)
                throw new ArgumentOutOfRangeException(nameof(currentPage));

            string relEndpoint = GetCrossTransferHistoryEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 1),
            };

            QueryBuilder qs = new QueryBuilder(261);
            if (!String.IsNullOrWhiteSpace(crossAsset))
                qs.AddParameter("asset", CommonUtility.NormalizeSymbol(crossAsset));
            if (direction != null)
                qs.AddParameter("type", Format(direction.Value));
            if (startTime != null)
                qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
            if (endTime != null)
                qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
            if (currentPage != null)
                qs.AddParameter("current", currentPage.Value);
            if (pageSize != null)
                qs.AddParameter("size", pageSize.Value);
            if (isArchived != null)
                qs.AddParameter("archived", isArchived.Value);

            return new DeferredQuery<ResultsPage<TransferTransaction>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseCrossTransferList,
                parseArgs: pageSize != null ? pageSize.Value : DefaultResultPageSize,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets cross margin transfer history asynchronously with the specified parameters.
        /// <para>The records are returned ordered by time from newer to older.</para>
        /// </summary>
        public Task<ResultsPage<TransferTransaction>> GetCrossTrasferHistoryAsync(
            string crossAsset = null, TransferDirection? direction = null,
            DateTime? startTime = null, DateTime? endTime = null,
            int? currentPage = null, byte? pageSize = null, bool? isArchived = null, CancellationToken ct = default)
        {
            using (IDeferredQuery<ResultsPage<TransferTransaction>> query =
                PrepareGetCrossTrasferHistory(crossAsset, direction, startTime, endTime, currentPage, pageSize, isArchived))
            {
                return query.ExecuteAsync(ct);
            }
        }

        public ResultsPage<TransferTransaction> ParseCrossTransferList(byte[] data, object parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);

            ResultsPage<TransferTransaction> resultList =
                new ResultsPage<TransferTransaction>(parseArgs is int expCount ? expCount : 100);
            bool wereRowsProvided = false;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string outerPropName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(outerPropName);

                switch (outerPropName)
                {
                    case "rows":
                        ParseUtility.ValidateArrayStartToken(ref reader);

                        // Prepare the buffer variables and the schema validator.
                        string asset = null;
                        decimal quantity = 0.0m;
                        TransactionStatus status = default;
                        DateTime timestamp = DateTime.MinValue;
                        long id = 0;
                        AccountType source = default, target = default;
                        ParseSchemaValidator validator = new ParseSchemaValidator(6);

                        // Parse each transaction in the array.
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            ParseUtility.ValidateObjectStartToken(ref reader);

                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                ParseUtility.ValidatePropertyNameToken(ref reader);
                                string propName = reader.GetString();

                                if (!reader.Read())
                                    throw ParseUtility.GenerateNoPropertyValueException(propName);

                                switch (propName)
                                {
                                    case "amount":
                                        ParseUtility.ParseDecimal(propName, reader.GetString(), out quantity);
                                        validator.RegisterProperty(0);
                                        break;
                                    case "asset":
                                        asset = reader.GetString();
                                        validator.RegisterProperty(1);
                                        break;
                                    case "status":
                                        status = ParseTransactionStatus(reader.GetString());
                                        validator.RegisterProperty(2);
                                        break;
                                    case "timestamp":
                                        timestamp = CommonUtility.ConvertToDateTime(reader.GetInt64());
                                        validator.RegisterProperty(3);
                                        break;
                                    case "txId":
                                        id = reader.GetInt64();
                                        validator.RegisterProperty(4);
                                        break;
                                    case "type":
                                        TransferDirection dir = ParseTransferDirection(reader.GetString());
                                        if (dir == TransferDirection.RollIn)
                                        {
                                            source = AccountType.Unknown;
                                            target = AccountType.CrossMargin;
                                        }
                                        else if (dir == TransferDirection.RollOut)
                                        {
                                            source = AccountType.CrossMargin;
                                            target = AccountType.Unknown;
                                        }
                                        else
                                        {
                                            throw new NotImplementedException($"Unable to interpret the transfer direction \"{dir}\".");
                                        }
                                        validator.RegisterProperty(5);
                                        break;
                                    case "clientTag":
                                        reader.Skip(); // The property is not stored.
                                        break;
                                    default:
                                        PostLogMessage(
                                            LogLevel.Warning,
                                            $"An unknown property \"{propName}\" of the cross margin transfer was encountered.");
                                        reader.Skip();
                                        break;
                                }
                            }

                            // Ensure that all the essential properties were provided.
                            if (!validator.IsComplete())
                            {
                                const string objName = "cross margin transfer";
                                int missingPropNum = validator.GetMissingPropertyNumber();
                                switch (missingPropNum)
                                {
                                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "quantity");
                                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "asset");
                                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "status");
                                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "timestamp");
                                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "id");
                                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "direction");
                                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                                }
                            }

                            // Add the transfer item to the result list.
                            resultList.Add(new TransferTransaction(id, asset, quantity, timestamp, status, source, target));
                            validator.Reset();
                        }

                        wereRowsProvided = true;
                        break;

                    case "total":
                        resultList.Total = reader.GetInt32();
                        break;

                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{outerPropName}\" of the cross margin transfer list was encountered.");
                        reader.Skip();
                        break;
                }
            }

            if (!wereRowsProvided)
                throw ParseUtility.GenerateMissingPropertyException("cross margin transfer list", "rows");

            return resultList;
        }

        // Isolated margin transfer history.
        /// <summary>
        /// Prepares a query for isolated margin transfer history with the specified parameters.
        /// <para>The records are returned ordered by time from newer to older.</para>
        /// </summary>
        /// <param name="isolatedSymbol">The isolated margin symbol to fetch transactions for.</param>
        /// <param name="asset">The asset to request transactions for. 
        /// <para>If not specified, transactions for all assets are returned.</para></param>
        /// <param name="direction">The direction of trasnfer transaction to fetch.
        /// <para>If not specified, transactions of all directions will be returned.</para>
        /// </param>
        /// <param name="startTime">
        /// The historical date and time to request transactions from. 
        /// <para>If not specified, the latest transactions before the <paramref name="endTime"/> or the current time will be returned.</para>
        /// </param>
        /// <param name="endTime">The historical date and time to request transactions prior.
        /// <para>If not specified, transactions prior the current time will be returned.</para>
        /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified,
        /// the interval between them must not exceed <see cref="MaxTransactionLookupInterval"/> (30 days).</para>
        /// </param>
        /// <param name="currentPage">A number of the lookup page in the results list to return (starting from 1).
        /// <para>If not specified, the 1st page will be returned.</para></param>
        /// <param name="pageSize">The limit of transactions per page to return.
        /// <para>If not specified, the default limit (10) will be used. The maximum value is <see cref="MaxResultPageSize"/> (100).</para></param>
        /// <param name="isArchived">Specifies whether to look for archived data from 6 months ago.
        /// <para>If not specified, <see langword="false"/> will be used.</para></param>
        public IDeferredQuery<ResultsPage<TransferTransaction>> PrepareGetIsolatedTrasferHistory(
            string isolatedSymbol, string asset = null, TransferDirection? direction = null,
            DateTime? startTime = null, DateTime? endTime = null,
            int? currentPage = null, byte? pageSize = null, bool? isArchived = null)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));
            ValidateTransactionHistoryInterval(ref startTime, ref endTime);
            if (pageSize < 1 || pageSize > MaxResultPageSize)
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            if (currentPage < 1)
                throw new ArgumentOutOfRangeException(nameof(currentPage));

            string relEndpoint = GetIsolatedTransferHistoryEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 1),
            };

            QueryBuilder qs = new QueryBuilder(279);
            qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(isolatedSymbol));
            if (!String.IsNullOrWhiteSpace(asset))
                qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            if (direction != null)
            {
                if (direction.Value == TransferDirection.RollIn)
                    qs.AddParameter("transTo", Format(AccountType.IsolatedMargin));
                else if (direction.Value == TransferDirection.RollOut)
                    qs.AddParameter("transFrom", Format(AccountType.IsolatedMargin));
                else
                    throw new NotImplementedException($"The transfer direction \"{direction.Value}\" is not implemented.");
            }
            if (startTime != null)
                qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
            if (endTime != null)
                qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
            if (currentPage != null)
                qs.AddParameter("current", currentPage.Value);
            if (pageSize != null)
                qs.AddParameter("size", pageSize.Value);
            if (isArchived != null)
                qs.AddParameter("archived", isArchived.Value);

            return new DeferredQuery<ResultsPage<TransferTransaction>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseIsolatedTransferList,
                parseArgs: pageSize != null ? pageSize.Value : DefaultResultPageSize,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets isolated margin transfer history asynchronously with the specified parameters.
        /// <para>The records are returned ordered by time from newer to older.</para>
        /// </summary>
        public Task<ResultsPage<TransferTransaction>> GetIsolatedTrasferHistoryAsync(
            string isolatedSymbol, string asset = null, TransferDirection? direction = null,
            DateTime? startTime = null, DateTime? endTime = null,
            int? currentPage = null, byte? pageSize = null, bool? isArchived = null, CancellationToken ct = default)
        {
            using (IDeferredQuery<ResultsPage<TransferTransaction>> query =
                PrepareGetIsolatedTrasferHistory(isolatedSymbol, asset, direction, startTime, endTime, currentPage, pageSize, isArchived))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private ResultsPage<TransferTransaction> ParseIsolatedTransferList(byte[] data, object parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);

            ResultsPage<TransferTransaction> resultList =
                new ResultsPage<TransferTransaction>(parseArgs is int expCount ? expCount : 100);
            bool wereRowsProvided = false;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string outerPropName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(outerPropName);

                switch (outerPropName)
                {
                    case "rows":
                        ParseUtility.ValidateArrayStartToken(ref reader);

                        // Prepare the buffer variables and the schema validator.
                        string asset = null;
                        decimal quantity = 0.0m;
                        TransactionStatus status = default;
                        DateTime timestamp = DateTime.MinValue;
                        long id = 0;
                        AccountType source = default, target = default;
                        ParseSchemaValidator validator = new ParseSchemaValidator(7);

                        // Parse each transaction in the array.
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            ParseUtility.ValidateObjectStartToken(ref reader);

                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                ParseUtility.ValidatePropertyNameToken(ref reader);
                                string propName = reader.GetString();

                                if (!reader.Read())
                                    throw ParseUtility.GenerateNoPropertyValueException(propName);

                                switch (propName)
                                {
                                    case "amount":
                                        ParseUtility.ParseDecimal(propName, reader.GetString(), out quantity);
                                        validator.RegisterProperty(0);
                                        break;
                                    case "asset":
                                        asset = reader.GetString();
                                        validator.RegisterProperty(1);
                                        break;
                                    case "status":
                                        status = ParseTransactionStatus(reader.GetString());
                                        validator.RegisterProperty(2);
                                        break;
                                    case "timestamp":
                                        timestamp = CommonUtility.ConvertToDateTime(reader.GetInt64());
                                        validator.RegisterProperty(3);
                                        break;
                                    case "txId":
                                        id = reader.GetInt64();
                                        validator.RegisterProperty(4);
                                        break;
                                    case "transFrom":
                                        source = ParseAccountType(reader.GetString());
                                        validator.RegisterProperty(5);
                                        break;
                                    case "transTo":
                                        source = ParseAccountType(reader.GetString());
                                        validator.RegisterProperty(6);
                                        break;
                                    case "clientTag":
                                        reader.Skip(); // The property is not stored.
                                        break;
                                    default:
                                        PostLogMessage(
                                            LogLevel.Warning,
                                            $"An unknown property \"{propName}\" of the cross margin transfer was encountered.");
                                        reader.Skip();
                                        break;
                                }
                            }

                            // Ensure that all the essential properties were provided.
                            if (!validator.IsComplete())
                            {
                                const string objName = "isolated margin transfer";
                                int missingPropNum = validator.GetMissingPropertyNumber();
                                switch (missingPropNum)
                                {
                                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "quantity");
                                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "asset");
                                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "status");
                                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "timestamp");
                                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "id");
                                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "source");
                                    case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "target");
                                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                                }
                            }

                            // Add the transfer item to the result list.
                            resultList.Add(new TransferTransaction(id, asset, quantity, timestamp, status, source, target));
                            validator.Reset();
                        }

                        wereRowsProvided = true;
                        break;

                    case "total":
                        resultList.Total = reader.GetInt32();
                        break;

                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{outerPropName}\" of the isolated margin transfer list was encountered.");
                        reader.Skip();
                        break;
                }
            }

            if (!wereRowsProvided)
                throw ParseUtility.GenerateMissingPropertyException("isolated margin transfer list", "rows");

            return resultList;
        }

        // Borrow asset.
        /// <summary>
        /// Prepares a query for borrowing the specified cross margin asset.
        /// </summary>
        /// <param name="crossAsset">The cross margin asset to borrow.</param>
        /// <param name="quantity">The asset quantity to borrow.</param>
        public IDeferredQuery<long> PrepareBorrowAsset(string crossAsset, decimal quantity)
        {
            if (String.IsNullOrWhiteSpace(crossAsset))
                throw new ArgumentNullException(nameof(crossAsset));
            if (quantity <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(quantity));

            return PrepareBorrowAsset(false, null, crossAsset, quantity);
        }

        /// <summary>
        /// Prepares a query for borrowing the specified isolated margin asset.
        /// </summary>
        /// <param name="isolatedSymbol">The isolated margin symbol to borrow for.</param>
        /// <param name="asset">The asset to borrow.</param>
        /// <param name="quantity">The asset quantity to borrow.</param>
        public IDeferredQuery<long> PrepareBorrowAsset(string isolatedSymbol, string asset, decimal quantity)
        {
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));
            if (String.IsNullOrWhiteSpace(asset))
                throw new ArgumentNullException(nameof(asset));
            if (quantity <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(quantity));

            return PrepareBorrowAsset(true, isolatedSymbol, asset, quantity);
        }

        private IDeferredQuery<long> PrepareBorrowAsset(bool isIsolated, string symbol, string asset, decimal quantity)
        {
            ThrowIfNotRunning();

            string relEndpoint = PostBorrowEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.UID);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.UID), 3000),
            };

            QueryBuilder qs = new QueryBuilder(187);
            qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            if (isIsolated)
            {
                qs.AddParameter("isIsolated", true);
                qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            }
            else
            {
                qs.AddParameter("isIsolated", false);
            }
            qs.AddParameter("amount", quantity);

            return new DeferredQuery<long>(
                query: new QueryParams(HttpMethod.POST, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseTransactionId,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        // Repay asset.
        /// <summary>
        /// Prepares a query for repaying the specified cross margin asset.
        /// </summary>
        /// <param name="crossAsset">The cross margin asset to repay.</param>
        /// <param name="quantity">The asset quantity to repay.</param>
        public IDeferredQuery<long> PrepareRepayAsset(string crossAsset, decimal quantity)
        {
            if (String.IsNullOrWhiteSpace(crossAsset))
                throw new ArgumentNullException(nameof(crossAsset));
            if (quantity <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(quantity));

            return PrepareRepayAsset(false, null, crossAsset, quantity);
        }

        /// <summary>
        /// Prepares a query for repaying the specified isolated margin asset.
        /// </summary>
        /// <param name="isolatedSymbol">The isolated margin symbol to repay for.</param>
        /// <param name="asset">The asset to repay.</param>
        /// <param name="quantity">The asset quantity to repay.</param>
        public IDeferredQuery<long> PrepareRepayAsset(string isolatedSymbol, string asset, decimal quantity)
        {
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));
            if (String.IsNullOrWhiteSpace(asset))
                throw new ArgumentNullException(nameof(asset));
            if (quantity <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(quantity));

            return PrepareRepayAsset(true, isolatedSymbol, asset, quantity);
        }

        private IDeferredQuery<long> PrepareRepayAsset(bool isIsolated, string symbol, string asset, decimal quantity)
        {
            ThrowIfNotRunning();

            string relEndpoint = PostRepayEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.UID);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.UID), 3000),
            };

            QueryBuilder qs = new QueryBuilder(187);
            qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            if (isIsolated)
            {
                qs.AddParameter("isIsolated", true);
                qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            }
            else
            {
                qs.AddParameter("isIsolated", false);
            }
            qs.AddParameter("amount", quantity);

            return new DeferredQuery<long>(
                query: new QueryParams(HttpMethod.POST, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseTransactionId,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        // Get borrow records.
        /// <summary>
        /// Prepares a query for a cross margin borrow record with the specified transaction ID.
        /// </summary>
        /// <param name="crossAsset">The cross margin asset that the transaction was made for.</param>
        /// <param name="transactionId">The identifier of the transaction to fetch.</param>
        /// <param name="isArchived">Indicates whether to search data older than 6 months.</param>
        public IDeferredQuery<LoanTransaction> PrepareGetBorrowRecord(
            string crossAsset, long transactionId, bool? isArchived = null)
        {
            return PrepareGetSingleBorrowRecord(crossAsset, null, transactionId, isArchived);
        }

        /// <summary>
        /// Prepares a query for an isolated margin borrow record with the specified transaction ID.
        /// </summary>
        /// <param name="isolatedSymbol">The isolated margin symbol that the transaction was made for.</param>
        /// <param name="asset">The cross margin asset that the transaction was made for.</param>
        /// <param name="transactionId">The identifier of the transaction to fetch.</param>
        /// <param name="isArchived">Indicates whether to search data older than 6 months.</param>
        public IDeferredQuery<LoanTransaction> PrepareGetBorrowRecord(
            string isolatedSymbol, string asset, long transactionId, bool? isArchived = null)
        {
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));
            return PrepareGetSingleBorrowRecord(asset, isolatedSymbol, transactionId, isArchived);
        }

        private IDeferredQuery<LoanTransaction> PrepareGetSingleBorrowRecord(
            string asset, string isolatedSymbol, long transactionId, bool? isArchived)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(asset))
                throw new ArgumentNullException(nameof(asset));

            string relEndpoint = GetBorrowRecordEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            QueryBuilder qs = new QueryBuilder(198);
            qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            qs.AddParameter("txId", transactionId);
            if (isolatedSymbol != null)
                qs.AddParameter("isolatedSymbol", CommonUtility.NormalizeSymbol(isolatedSymbol));
            if (isArchived != null)
                qs.AddParameter("archived", isArchived.Value);

            return new DeferredQuery<LoanTransaction>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseSingleBorrowRecord,
                parseArgs: isolatedSymbol != null,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Prepares a query for cross margin borrow records with the specified parameters.
        /// <para>The records are returned ordered by time from newer to older.</para>
        /// </summary>
        /// <param name="crossAsset">The cross margin asset to request borrow records for.</param>
        /// <param name="startTime">The time to request borrow records from.</param>
        /// <param name="endTime">The time to request borrow records prior to.
        /// <para>If not specified, borrow records prior to the current time will be returned.</para>
        /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified,
        /// the interval between them must not exceed <see cref="MaxTransactionLookupInterval"/> (30 days).</para>
        /// </param>
        /// <param name="currentPage">A number of the lookup page in the results list to return (starting from 1).
        /// <para>If not specified, the 1st page will be returned.</para></param>
        /// <param name="pageSize">The limit of borrow records per page to return.
        /// <para>If not specified, the default limit (10) will be used. The maximum value is <see cref="MaxResultPageSize"/> (100).</para></param>
        /// <param name="isArchived">Specifies whether to look for archived data older than 6 months.
        /// <para>If not specified, <see langword="false"/> will be used.</para></param>
        /// <returns>A deferred query for a paginated list of borrow records.</returns>
        public IDeferredQuery<ResultsPage<LoanTransaction>> PrepareGetBorrowRecords(
            string crossAsset, DateTime startTime, DateTime? endTime,
            int? currentPage, byte? pageSize, bool? isArchived = null)
        {
            return PrepareGetBorrowRecordList(crossAsset, null, startTime, endTime, currentPage, pageSize, isArchived);
        }

        /// <summary>
        /// Prepares a query for isolated margin borrow records with the specified parameters.
        /// <para>The records are returned ordered by time from newer to older.</para>
        /// </summary>
        /// <param name="isolatedSymbol">The isolated margin symbol to fetch borrow records for.</param>
        /// <param name="asset">The asset to request borrow records for.</param>
        /// <param name="startTime">The time to request borrow records from.</param>
        /// <param name="endTime">The time to request borrow records prior to.
        /// <para>If not specified, borrow records prior to the current time will be returned.</para>
        /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified,
        /// the interval between them must not exceed <see cref="MaxTransactionLookupInterval"/> (30 days).</para>
        /// </param>
        /// <param name="currentPage">A number of the lookup page in the results list to return (starting from 1).
        /// <para>If not specified, the 1st page will be returned.</para></param>
        /// <param name="pageSize">The limit of borrow records per page to return.
        /// <para>If not specified, the default limit (10) will be used. The maximum value is <see cref="MaxResultPageSize"/> (100).</para></param>
        /// <param name="isArchived">Specifies whether to look for archived data older than 6 months.
        /// <para>If not specified, <see langword="false"/> will be used.</para></param>
        /// <returns>A deferred query for a paginated list of borrow records.</returns>
        public IDeferredQuery<ResultsPage<LoanTransaction>> PrepareGetBorrowRecords(
            string isolatedSymbol, string asset, DateTime startTime, DateTime? endTime,
            int? currentPage = null, byte? pageSize = null, bool? isArchived = null)
        {
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));

            return PrepareGetBorrowRecordList(asset, isolatedSymbol, startTime, endTime, currentPage, pageSize, isArchived);
        }

        private IDeferredQuery<ResultsPage<LoanTransaction>> PrepareGetBorrowRecordList(
            string asset, string isolatedSymbol, DateTime? startTime, DateTime? endTime,
            int? currentPage, byte? pageSize, bool? isArchived)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(asset))
                throw new ArgumentNullException(nameof(asset));
            ValidateTransactionHistoryInterval(ref startTime, ref endTime);
            if (pageSize < 1 || pageSize > MaxResultPageSize)
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            if (currentPage < 1)
                throw new ArgumentOutOfRangeException(nameof(currentPage));

            string relEndpoint = GetBorrowRecordEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            QueryBuilder qs = new QueryBuilder(241);
            qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            if (isolatedSymbol != null)
                qs.AddParameter("isolatedSymbol", CommonUtility.NormalizeSymbol(isolatedSymbol));
            if (startTime != null)
                qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
            if (endTime != null)
                qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
            if (currentPage != null)
                qs.AddParameter("current", currentPage.Value);
            if (pageSize != null)
                qs.AddParameter("size", pageSize.Value);
            if (isArchived != null)
                qs.AddParameter("archived", isArchived.Value);

            return new DeferredQuery<ResultsPage<LoanTransaction>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseBorrowRecordList,
                parseArgs: isolatedSymbol != null,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        private LoanTransaction ParseSingleBorrowRecord(byte[] data, object parseArgs)
        {
            bool isIsolated = (bool)parseArgs;
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ResultsPage<LoanTransaction> result = ParseBorrowRecordList(ref reader, isIsolated, 1);
            if (result.Count == 0)
                throw new JsonException($"The borrow records response does not contain any record object.");

            return result[0];
        }

        private ResultsPage<LoanTransaction> ParseBorrowRecordList(byte[] data, object parseArgs)
        {
            bool isIsolated = (bool)parseArgs;
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            return ParseBorrowRecordList(ref reader, isIsolated, 100);
        }

        private ResultsPage<LoanTransaction> ParseBorrowRecordList(ref Utf8JsonReader reader, bool isIsolated, int expectedCount)
        {
            ParseUtility.ReadObjectStart(ref reader);
            ResultsPage<LoanTransaction> resultList = new ResultsPage<LoanTransaction>(expectedCount);
            bool wereRowsParsed = false;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string outerPropName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(outerPropName);

                switch (outerPropName)
                {
                    case "rows":
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            ParseUtility.ValidateObjectStartToken(ref reader);

                            string symbol = null, asset = null;
                            long id = 0;
                            decimal principal = 0.0m;
                            DateTime time = DateTime.MinValue;
                            TransactionStatus status = default;
                            ParseSchemaValidator validator = new ParseSchemaValidator(6);

                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                ParseUtility.ValidatePropertyNameToken(ref reader);
                                string propName = reader.GetString();

                                if (!reader.Read())
                                    throw new JsonException($"A value of the property \"{propName}\" was expected " +
                                        $"but \"{reader.TokenType}\" encountered.");

                                switch (propName)
                                {
                                    case "isolatedSymbol":
                                        symbol = reader.GetString();
                                        validator.RegisterProperty(0);
                                        break;
                                    case "txId":
                                        id = reader.GetInt64();
                                        validator.RegisterProperty(1);
                                        break;
                                    case "asset":
                                        asset = reader.GetString();
                                        validator.RegisterProperty(2);
                                        break;
                                    case "principal":
                                        ParseUtility.ParseDecimal(propName, reader.GetString(), out principal);
                                        validator.RegisterProperty(3);
                                        break;
                                    case "timestamp":
                                        time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                                        validator.RegisterProperty(4);
                                        break;
                                    case "status":
                                        status = ParseTransactionStatus(reader.GetString());
                                        validator.RegisterProperty(5);
                                        break;
                                    case "clientTag":
                                        // The property is not stored.
                                        break;
                                    default:
                                        PostLogMessage(
                                            LogLevel.Warning,
                                            $"An unknown borrow record property \"{propName}\" was encountered.");
                                        reader.Skip();
                                        break;
                                }
                            }

                            if (isIsolated)
                            {
                                symbol = null;
                                validator.RegisterProperty(0);
                            }

                            if (!validator.IsComplete())
                            {
                                const string objName = "borrow record";
                                int missingPropNum = validator.GetMissingPropertyNumber();
                                switch (missingPropNum)
                                {
                                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "isolated symbol");
                                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "ID");
                                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "asset");
                                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "principal");
                                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "timestamp");
                                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "status");
                                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                                }
                            }

                            resultList.Add(new LoanTransaction(id, symbol, asset, principal, time, status));
                        }
                        wereRowsParsed = true;
                        break;

                    case "total":
                        resultList.Total = reader.GetInt32();
                        break;

                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{outerPropName}\" was encountered in the borrow records response.");
                        reader.Skip();
                        break;
                }
            }

            if (!wereRowsParsed)
                throw ParseUtility.GenerateMissingPropertyException("borrow records response", "rows");

            return resultList;
        }

        // Get repay records.
        /// <summary>
        /// Prepares a query for a cross margin repayment record with the specified transaction ID.
        /// </summary>
        /// <param name="crossAsset">The cross margin asset that the transaction was made for.</param>
        /// <param name="transactionId">The identifier of the transaction to fetch.</param>
        /// <param name="isArchived">Indicates whether to search data older than 6 months.</param>
        public IDeferredQuery<RepayTransaction> PrepareGetRepayRecord(
            string crossAsset, long transactionId, bool? isArchived = null)
        {
            return PrepareGetSingleRepayRecord(crossAsset, null, transactionId, isArchived);
        }

        /// <summary>
        /// Prepares a query for an isolated margin repayment record with the specified transaction ID.
        /// </summary>
        /// <param name="isolatedSymbol">The isolated margin symbol that the transaction was made for.</param>
        /// <param name="asset">The cross margin asset that the transaction was made for.</param>
        /// <param name="transactionId">The identifier of the transaction to fetch.</param>
        /// <param name="isArchived">Indicates whether to search data older than 6 months.</param>
        public IDeferredQuery<RepayTransaction> PrepareGetRepayRecord(
            string isolatedSymbol, string asset, long transactionId, bool? isArchived = null)
        {
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));
            return PrepareGetSingleRepayRecord(asset, isolatedSymbol, transactionId, isArchived);
        }

        private IDeferredQuery<RepayTransaction> PrepareGetSingleRepayRecord(
            string asset, string isolatedSymbol, long transactionId, bool? isArchived)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(asset))
                throw new ArgumentNullException(nameof(asset));

            string relEndpoint = GetRepayRecordEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            QueryBuilder qs = new QueryBuilder(198);
            qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            qs.AddParameter("txId", transactionId);
            if (isolatedSymbol != null)
                qs.AddParameter("isolatedSymbol", CommonUtility.NormalizeSymbol(isolatedSymbol));
            if (isArchived != null)
                qs.AddParameter("archived", isArchived.Value);

            return new DeferredQuery<RepayTransaction>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseRepayRecord,
                parseArgs: isolatedSymbol != null,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Prepares a query for cross margin repayment records with the specified parameters.
        /// <para>The records are returned ordered by time from newer to older.</para>
        /// </summary>
        /// <param name="crossAsset">The cross margin asset to request repayment records for.</param>
        /// <param name="startTime">The time to request repayment records from.</param>
        /// <param name="endTime">The time to request repayment records prior to.
        /// <para>If not specified, repayment records prior to the current time will be returned.</para>
        /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified,
        /// the interval between them must not exceed <see cref="MaxTransactionLookupInterval"/> (30 days).</para>
        /// </param>
        /// <param name="currentPage">A number of the lookup page in the results list to return (starting from 1).
        /// <para>If not specified, the 1st page will be returned.</para></param>
        /// <param name="pageSize">The limit of repayment records per page to return.
        /// <para>If not specified, the default limit (10) will be used. The maximum value is <see cref="MaxResultPageSize"/> (100).</para></param>
        /// <param name="isArchived">Specifies whether to look for archived data older than 6 months.
        /// <para>If not specified, <see langword="false"/> will be used.</para></param>
        /// <returns>A deferred query for a paginated list of repayment records.</returns>
        public IDeferredQuery<ResultsPage<RepayTransaction>> PrepareGetRepayRecords(
            string crossAsset, DateTime startTime, DateTime? endTime,
            int? currentPage, byte? pageSize, bool? isArchived = null)
        {
            return PrepareGetRepayRecordList(crossAsset, null, startTime, endTime, currentPage, pageSize, isArchived);
        }

        /// <summary>
        /// Prepares a query for isolated margin repayment records with the specified parameters.
        /// <para>The records are returned ordered by time from newer to older.</para>
        /// </summary>
        /// <param name="isolatedSymbol">The isolated margin symbol to fetch repayment records for.</param>
        /// <param name="asset">The asset to request repayment records for.</param>
        /// <param name="startTime">The time to request repayment records from.</param>
        /// <param name="endTime">The time to request repayment records prior to.
        /// <para>If not specified, repayment records prior to the current time will be returned.</para>
        /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified,
        /// the interval between them must not exceed <see cref="MaxTransactionLookupInterval"/> (30 days).</para>
        /// </param>
        /// <param name="currentPage">A number of the lookup page in the results list to return (starting from 1).
        /// <para>If not specified, the 1st page will be returned.</para></param>
        /// <param name="pageSize">The limit of repayment records per page to return.
        /// <para>If not specified, the default limit (10) will be used. The maximum value is <see cref="MaxResultPageSize"/> (100).</para></param>
        /// <param name="isArchived">Specifies whether to look for archived data older than 6 months.
        /// <para>If not specified, <see langword="false"/> will be used.</para></param>
        /// <returns>A deferred query for a paginated list of repayment records.</returns>
        public IDeferredQuery<ResultsPage<RepayTransaction>> PrepareGetRepayRecords(
            string isolatedSymbol, string asset, DateTime startTime, DateTime? endTime,
            int? currentPage = null, byte? pageSize = null, bool? isArchived = null)
        {
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));

            return PrepareGetRepayRecordList(asset, isolatedSymbol, startTime, endTime, currentPage, pageSize, isArchived);
        }

        private IDeferredQuery<ResultsPage<RepayTransaction>> PrepareGetRepayRecordList(
            string asset, string isolatedSymbol, DateTime? startTime, DateTime? endTime,
            int? currentPage, byte? pageSize, bool? isArchived)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(asset))
                throw new ArgumentNullException(nameof(asset));
            ValidateTransactionHistoryInterval(ref startTime, ref endTime);
            if (pageSize < 1 || pageSize > MaxResultPageSize)
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            if (currentPage < 1)
                throw new ArgumentOutOfRangeException(nameof(currentPage));

            string relEndpoint = GetRepayRecordEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            QueryBuilder qs = new QueryBuilder(241);
            qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            if (isolatedSymbol != null)
                qs.AddParameter("isolatedSymbol", CommonUtility.NormalizeSymbol(isolatedSymbol));
            if (startTime != null)
                qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
            if (endTime != null)
                qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
            if (currentPage != null)
                qs.AddParameter("current", currentPage.Value);
            if (pageSize != null)
                qs.AddParameter("size", pageSize.Value);
            if (isArchived != null)
                qs.AddParameter("archived", isArchived.Value);

            return new DeferredQuery<ResultsPage<RepayTransaction>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseRepayRecordList,
                parseArgs: isolatedSymbol != null,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        private RepayTransaction ParseRepayRecord(byte[] data, object parseArgs)
        {
            bool isIsolated = (bool)parseArgs;
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ResultsPage<RepayTransaction> result = ParseRepayRecordList(ref reader, isIsolated, 1);
            if (result.Count == 0)
                throw new JsonException($"The repay records response does not contain any record object.");

            return result[0];
        }

        private ResultsPage<RepayTransaction> ParseRepayRecordList(byte[] data, object parseArgs)
        {
            bool isIsolated = (bool)parseArgs;
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            return ParseRepayRecordList(ref reader, isIsolated, 100);
        }

        private ResultsPage<RepayTransaction> ParseRepayRecordList(ref Utf8JsonReader reader, bool isIsolated, int expectedCount)
        {
            ParseUtility.ReadObjectStart(ref reader);
            ResultsPage<RepayTransaction> resultList = new ResultsPage<RepayTransaction>(expectedCount);
            bool wereRowsParsed = false;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string outerPropName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(outerPropName);

                switch (outerPropName)
                {
                    case "rows":
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            ParseUtility.ValidateObjectStartToken(ref reader);

                            string symbol = null, asset = null;
                            long id = 0;
                            decimal quantity = 0.0m, principal = 0.0m, interest = 0.0m;
                            DateTime time = DateTime.MinValue;
                            TransactionStatus status = default;
                            ParseSchemaValidator validator = new ParseSchemaValidator(8);

                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                            {
                                ParseUtility.ValidatePropertyNameToken(ref reader);
                                string propName = reader.GetString();

                                if (!reader.Read())
                                    throw new JsonException($"A value of the property \"{propName}\" was expected " +
                                        $"but \"{reader.TokenType}\" encountered.");

                                switch (propName)
                                {
                                    case "isolatedSymbol":
                                        symbol = reader.GetString();
                                        validator.RegisterProperty(0);
                                        break;
                                    case "txId":
                                        id = reader.GetInt64();
                                        validator.RegisterProperty(1);
                                        break;
                                    case "asset":
                                        asset = reader.GetString();
                                        validator.RegisterProperty(2);
                                        break;
                                    case "amount":
                                        ParseUtility.ParseDecimal(propName, reader.GetString(), out quantity);
                                        validator.RegisterProperty(3);
                                        break;
                                    case "principal":
                                        ParseUtility.ParseDecimal(propName, reader.GetString(), out principal);
                                        validator.RegisterProperty(4);
                                        break;
                                    case "interest":
                                        ParseUtility.ParseDecimal(propName, reader.GetString(), out interest);
                                        validator.RegisterProperty(5);
                                        break;
                                    case "timestamp":
                                        time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                                        validator.RegisterProperty(6);
                                        break;
                                    case "status":
                                        status = ParseTransactionStatus(reader.GetString());
                                        validator.RegisterProperty(7);
                                        break;
                                    case "clientTag":
                                        // The property is not stored.
                                        break;
                                    default:
                                        PostLogMessage(
                                            LogLevel.Warning,
                                            $"An unknown repay record property \"{propName}\" was encountered.");
                                        reader.Skip();
                                        break;
                                }
                            }

                            if (isIsolated)
                            {
                                symbol = null;
                                validator.RegisterProperty(0);
                            }

                            if (!validator.IsComplete())
                            {
                                const string objName = "repay record";
                                int missingPropNum = validator.GetMissingPropertyNumber();
                                switch (missingPropNum)
                                {
                                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "isolated symbol");
                                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "ID");
                                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "asset");
                                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "quantity");
                                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "principal");
                                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "interest");
                                    case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "timestamp");
                                    case 7: throw ParseUtility.GenerateMissingPropertyException(objName, "status");
                                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                                }
                            }

                            resultList.Add(new RepayTransaction(id, symbol, asset, quantity, principal, interest, time, status));
                        }
                        wereRowsParsed = true;
                        break;

                    case "total":
                        resultList.Total = reader.GetInt32();
                        break;

                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{outerPropName}\" was encountered in the repay records response.");
                        reader.Skip();
                        break;
                }
            }

            if (!wereRowsParsed)
                throw ParseUtility.GenerateMissingPropertyException("repay records response", "rows");

            return resultList;
        }

        // Borrow limit.
        /// <summary>
        /// Prepares a query for borrow limits for the specified asset in the cross margin account.
        /// </summary>
        /// <param name="crossAsset">The cross margin asset to get a borrow limit for.</param>
        public IDeferredQuery<BorrowLimitInfo> PrepareGetBorrowLimit(string crossAsset)
        {
            if (String.IsNullOrWhiteSpace(crossAsset))
                throw new ArgumentNullException(nameof(crossAsset));

            return PrepareGetBorrowLimitPrivate(crossAsset, null);
        }

        /// <summary>
        /// Gets borrow limits for the specified asset in the cross margin account asynchronously.
        /// </summary>
        /// <param name="crossAsset">The cross margin asset to get a borrow limit for.</param>
        public Task<BorrowLimitInfo> GetBorrowLimitAsync(string crossAsset, CancellationToken ct = default)
        {
            using (IDeferredQuery<BorrowLimitInfo> query = PrepareGetBorrowLimit(crossAsset))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for borrow limits for the specified asset in an isolated margin account.
        /// </summary>
        /// <param name="isolatedSymbol">An isolated trading pair.</param>
        /// <param name="asset">The asset of the trading pair to get a borrow limit for.</param>
        public IDeferredQuery<BorrowLimitInfo> PrepareGetBorrowLimit(string isolatedSymbol, string asset)
        {
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));
            if (String.IsNullOrWhiteSpace(asset))
                throw new ArgumentNullException(nameof(asset));

            return PrepareGetBorrowLimitPrivate(asset, isolatedSymbol);
        }

        /// <summary>
        /// Gets borrow limits for the specified asset in an isolated margin account asynchronously.
        /// </summary>
        /// <param name="isolatedSymbol">An isolated trading pair.</param>
        /// <param name="asset">The asset of the trading pair to get a borrow limit for.</param>
        public Task<BorrowLimitInfo> GetBorrowLimitAsync(string isolatedSymbol, string asset, CancellationToken ct = default)
        {
            using (IDeferredQuery<BorrowLimitInfo> query = PrepareGetBorrowLimit(isolatedSymbol, asset))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private IDeferredQuery<BorrowLimitInfo> PrepareGetBorrowLimitPrivate(string asset, string isolatedSymbol = null)
        {
            ThrowIfNotRunning();

            string relEndpoint = GetMaxBorrowEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 50),
            };

            QueryBuilder qs = new QueryBuilder(158);
            qs.AddParameter("asset", CommonUtility.NormalizeSymbol(asset));
            if (isolatedSymbol != null)
                qs.AddParameter("isolatedSymbol", CommonUtility.NormalizeSymbol(isolatedSymbol));

            return new DeferredQuery<BorrowLimitInfo>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseBorrowLimitInfo,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        private BorrowLimitInfo ParseBorrowLimitInfo(byte[] data, object parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);

            decimal stateLimit = 0.0m, levelLimit = 0.0m;
            ParseSchemaValidator validator = new ParseSchemaValidator(2);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);

                switch (propName)
                {
                    case "amount":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out stateLimit);
                        validator.RegisterProperty(0);
                        break;
                    case "borrowLimit":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out levelLimit);
                        validator.RegisterProperty(1);
                        break;
                    default:
                        throw ParseUtility.GenerateUnknownPropertyException(propName);
                }
            }

            if (!validator.IsComplete())
            {
                const string objName = "borrow limit";
                int missingPropNum = validator.GetMissingPropertyNumber();
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "state limit");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "level limit");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            return new BorrowLimitInfo(stateLimit, levelLimit);
        }

        // Post new order.
        /// <summary>
        /// Prepares a query for the order of the type <see cref="OrderType.Limit"/> with the specified order parameters.
        /// </summary>
        /// <param name="symbol">The symbol to post the order on.</param>
        /// <param name="isIsolated">The type of the margin account to post the order on (isolated or cross).</param>
        /// <param name="side">The order side.</param>
        /// <param name="tif">The time in force for the order.</param>
        /// <param name="price">The limit price to execute the order at.</param>
        /// <param name="quantity">The quantity of the base asset to trade.</param>
        /// <param name="id">The custom identifier for the order (optional).</param>
        /// <param name="sideEffect">The margin action to take along with the order (optional).</param>
        /// <param name="orderResponseType">The type of response desired (optional).
        /// The default value is <see cref="OrderResponseType.Full"/>.</param>
        public IDeferredQuery<MarginOrderResponseBase> PreparePostLimitOrder(
            string symbol, bool isIsolated, OrderSide side, TimeInForce tif, decimal price, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null)
        {
            return PreparePostOrder(
                symbol: symbol,
                isIsolated: isIsolated,
                side: side,
                type: OrderType.Limit,
                tif: tif,
                price: price,
                stopPrice: null,
                quantity: quantity,
                quoteQuantity: null,
                icebergQuantity: null,
                id: id,
                sideEffect: sideEffect,
                orderResponseType: orderResponseType);
        }

        /// <summary>
        /// Posts a new order of the type <see cref="OrderType.Limit"/> with the specified order parameters.
        /// </summary>
        public Task<MarginOrderResponseBase> PostLimitOrderAsync(
            string symbol, bool isIsolated, OrderSide side, TimeInForce tif, decimal price, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null,
            CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrderResponseBase> query = PreparePostLimitOrder(
                symbol, isIsolated, side, tif, price, quantity, id, sideEffect, orderResponseType))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for the order of the type <see cref="OrderType.LimitMaker"/> with the specified order parameters.
        /// </summary>
        /// <param name="symbol">The symbol to post the order on.</param>
        /// <param name="isIsolated">The type of the margin account to post the order on (isolated or cross).</param>
        /// <param name="side">The order side.</param>
        /// <param name="price">The limit price to execute the order at.</param>
        /// <param name="quantity">The quantity of the base asset to trade.</param>
        /// <param name="id">The custom identifier for the order (optional).</param>
        /// <param name="sideEffect">The margin action to take along with the order (optional).</param>
        /// <param name="orderResponseType">The type of response desired (optional).
        /// The default value is <see cref="OrderResponseType.Full"/>.</param>
        public IDeferredQuery<MarginOrderResponseBase> PreparePostLimitMakerOrder(
            string symbol, bool isIsolated, OrderSide side, decimal price, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null)
        {
            return PreparePostOrder(
                symbol: symbol,
                isIsolated: isIsolated,
                side: side,
                type: OrderType.LimitMaker,
                tif: null,
                price: price,
                stopPrice: null,
                quantity: quantity,
                quoteQuantity: null,
                icebergQuantity: null,
                id: id,
                sideEffect: sideEffect,
                orderResponseType: orderResponseType);
        }

        /// <summary>
        /// Posts a new order of the type <see cref="OrderType.LimitMaker"/> with the specified order parameters.
        /// </summary>
        public Task<MarginOrderResponseBase> PostLimitMakerOrderAsync(
            string symbol, bool isIsolated, OrderSide side, decimal price, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null,
            CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrderResponseBase> query = PreparePostLimitMakerOrder(
                symbol, isIsolated, side, price, quantity, id, sideEffect, orderResponseType))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for the order of the type <see cref="OrderType.Market"/> with the specified order parameters.
        /// <para>Either <paramref name="baseQuantity"/> or <paramref name="quoteQuantity"/> must be specified, but not both.</para>
        /// </summary>
        /// <param name="symbol">The symbol to post the order on.</param>
        /// <param name="isIsolated">The type of the margin account to post the order on (isolated or cross).</param>
        /// <param name="side">The order side.</param>
        /// <param name="baseQuantity">The quantity of the base asset to trade.</param>
        /// <param name="quoteQuantity">The quantity of the quote asset to trade.</param>
        /// <param name="id">The custom identifier for the order (optional).</param>
        /// <param name="sideEffect">The margin action to take along with the order (optional).</param>
        /// <param name="orderResponseType">The type of response desired (optional).
        /// The default value is <see cref="OrderResponseType.Full"/>.</param>
        public IDeferredQuery<MarginOrderResponseBase> PreparePostMarketOrder(
            string symbol, bool isIsolated, OrderSide side, decimal? baseQuantity, decimal? quoteQuantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null)
        {
            if ((baseQuantity == null) == (quoteQuantity == null))
            {
                throw new ArgumentException(
                    $"Exclusively one of the {nameof(baseQuantity)} and {nameof(quoteQuantity)} parameters must be provided " +
                    $"with a non-null value. However, the specified values are {baseQuantity} and {quoteQuantity} accordingly.");
            }

            return PreparePostOrder(
                symbol: symbol,
                isIsolated: isIsolated,
                side: side,
                type: OrderType.Market,
                tif: null,
                price: null,
                stopPrice: null,
                quantity: baseQuantity,
                quoteQuantity: quoteQuantity,
                icebergQuantity: null,
                id: id,
                sideEffect: sideEffect,
                orderResponseType: orderResponseType);
        }

        /// <summary>
        /// Posts a new order of the type <see cref="OrderType.Market"/> with the specified order parameters.
        /// </summary>
        public Task<MarginOrderResponseBase> PostMarketOrderAsync(
            string symbol, bool isIsolated, OrderSide side, decimal? baseQuantity, decimal? quoteQuantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null,
            CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrderResponseBase> query = PreparePostMarketOrder(
                symbol, isIsolated, side, baseQuantity, quoteQuantity, id, sideEffect, orderResponseType))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for the order of the type <see cref="OrderType.StopLossLimit"/> with the specified order parameters.
        /// </summary>
        /// <param name="symbol">The symbol to post the order on.</param>
        /// <param name="isIsolated">The type of the margin account to post the order on (isolated or cross).</param>
        /// <param name="side">The order side.</param>
        /// <param name="tif">The time in force for the order.</param>
        /// <param name="stopPrice">The price at which the stop loss order will be triggered.</param>
        /// <param name="executionPrice">The price at which the stop loss order will be executed once it is triggered.</param>
        /// <param name="quantity">The quantity of the base asset to trade.</param>
        /// <param name="id">The custom identifier for the order (optional).</param>
        /// <param name="sideEffect">The margin action to take along with the order (optional).</param>
        /// <param name="orderResponseType">The type of response desired (optional).
        /// The default value is <see cref="OrderResponseType.Ack"/>.</param>
        public IDeferredQuery<MarginOrderResponseBase> PreparePostStopLossLimitOrder(
            string symbol, bool isIsolated, OrderSide side, TimeInForce tif,
            decimal stopPrice, decimal executionPrice, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null)
        {
            return PreparePostOrder(
                symbol: symbol,
                isIsolated: isIsolated,
                side: side,
                type: OrderType.StopLossLimit,
                tif: tif,
                price: executionPrice,
                stopPrice: stopPrice,
                quantity: quantity,
                quoteQuantity: null,
                icebergQuantity: null,
                id: id,
                sideEffect: sideEffect,
                orderResponseType: orderResponseType);
        }

        /// <summary>
        /// Posts a new order of the type <see cref="OrderType.StopLossLimit"/> with the specified order parameters.
        /// </summary>
        public Task<MarginOrderResponseBase> PostStopLossLimitOrderAsync(
            string symbol, bool isIsolated, OrderSide side, TimeInForce tif,
            decimal stopPrice, decimal executionPrice, decimal quantity, string id = null,
            MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null, CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrderResponseBase> query = PreparePostStopLossLimitOrder(
                symbol, isIsolated, side, tif, stopPrice, executionPrice, quantity, id, sideEffect, orderResponseType))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for the order of the type <see cref="OrderType.StopLoss"/> with the specified order parameters.
        /// </summary>
        /// <param name="symbol">The symbol to post the order on.</param>
        /// <param name="isIsolated">The type of the margin account to post the order on (isolated or cross).</param>
        /// <param name="side">The order side.</param>
        /// <param name="stopPrice">The price at which the stop loss order will be triggered.</param>
        /// <param name="quantity">The quantity of the base asset to trade.</param>
        /// <param name="id">The custom identifier for the order (optional).</param>
        /// <param name="sideEffect">The margin action to take along with the order (optional).</param>
        /// <param name="orderResponseType">The type of response desired (optional).
        /// The default value is <see cref="OrderResponseType.Ack"/>.</param>
        public IDeferredQuery<MarginOrderResponseBase> PreparePostStopLossMarketOrder(
            string symbol, bool isIsolated, OrderSide side, decimal stopPrice, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null)
        {
            return PreparePostOrder(
                symbol: symbol,
                isIsolated: isIsolated,
                side: side,
                type: OrderType.StopLoss,
                tif: null,
                price: null,
                stopPrice: stopPrice,
                quantity: quantity,
                quoteQuantity: null,
                icebergQuantity: null,
                id: id,
                sideEffect: sideEffect,
                orderResponseType: orderResponseType);
        }

        /// <summary>
        /// Posts a new order of the type <see cref="OrderType.StopLoss"/> with the specified order parameters.
        /// </summary>
        public Task<MarginOrderResponseBase> PostStopLossMarketOrderAsync(
            string symbol, bool isIsolated, OrderSide side, decimal stopPrice, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null,
            CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrderResponseBase> query = PreparePostStopLossMarketOrder(
                symbol, isIsolated, side, stopPrice, quantity, id, sideEffect, orderResponseType))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for the order of the type <see cref="OrderType.TakeProfitLimit"/> with the specified order parameters.
        /// </summary>
        /// <param name="symbol">The symbol to post the order on.</param>
        /// <param name="isIsolated">The type of the margin account to post the order on (isolated or cross).</param>
        /// <param name="side">The order side.</param>
        /// <param name="tif">The time in force for the order.</param>
        /// <param name="takePrice">The price at which the take profit order will be triggered.</param>
        /// <param name="executionPrice">The price at which the stop loss order will be executed once it is triggered.</param>
        /// <param name="quantity">The quantity of the base asset to trade.</param>
        /// <param name="id">The custom identifier for the order (optional).</param>
        /// <param name="sideEffect">The margin action to take along with the order (optional).</param>
        /// <param name="orderResponseType">The type of response desired (optional).
        /// The default value is <see cref="OrderResponseType.Ack"/>.</param>
        public IDeferredQuery<MarginOrderResponseBase> PreparePostTakeProfitLimitOrder(
            string symbol, bool isIsolated, OrderSide side, TimeInForce tif,
            decimal takePrice, decimal executionPrice, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null)
        {
            return PreparePostOrder(
                symbol: symbol,
                isIsolated: isIsolated,
                side: side,
                type: OrderType.TakeProfitLimit,
                tif: tif,
                price: executionPrice,
                stopPrice: takePrice,
                quantity: quantity,
                quoteQuantity: null,
                icebergQuantity: null,
                id: id,
                sideEffect: sideEffect,
                orderResponseType: orderResponseType);
        }

        /// <summary>
        /// Posts a new order of the type <see cref="OrderType.TakeProfitLimit"/> with the specified order parameters.
        /// </summary>
        public Task<MarginOrderResponseBase> PostTakeProfitLimitOrderAsync(
            string symbol, bool isIsolated, OrderSide side, TimeInForce tif,
            decimal takePrice, decimal executionPrice, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null,
            CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrderResponseBase> query = PreparePostTakeProfitLimitOrder(
                symbol, isIsolated, side, tif, takePrice, executionPrice, quantity, id, sideEffect, orderResponseType))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for the order of the type <see cref="OrderType.TakeProfit"/> with the specified order parameters.
        /// </summary>
        /// <param name="symbol">The symbol to post the order on.</param>
        /// <param name="isIsolated">The type of the margin account to post the order on (isolated or cross).</param>
        /// <param name="side">The order side.</param>
        /// <param name="takePrice">The price at which the take profit order will be triggered.</param>
        /// <param name="quantity">The quantity of the base asset to trade.</param>
        /// <param name="id">The custom identifier for the order (optional).</param>
        /// <param name="sideEffect">The margin action to take along with the order (optional).</param>
        /// <param name="orderResponseType">The type of response desired (optional).
        /// The default value is <see cref="OrderResponseType.Ack"/>.</param>
        public IDeferredQuery<MarginOrderResponseBase> PreparePostTakeProfitMarketOrder(
            string symbol, bool isIsolated, OrderSide side, decimal takePrice, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null)
        {
            return PreparePostOrder(
                symbol: symbol,
                isIsolated: isIsolated,
                side: side,
                type: OrderType.TakeProfit,
                tif: null,
                price: null,
                stopPrice: takePrice,
                quantity: quantity,
                quoteQuantity: null,
                icebergQuantity: null,
                id: id,
                sideEffect: sideEffect,
                orderResponseType: orderResponseType);
        }

        /// <summary>
        /// Posts a new order of the type <see cref="OrderType.TakeProfit"/> with the specified order parameters.
        /// </summary>
        public Task<MarginOrderResponseBase> PostTakeProfitMarketOrderAsync(
            string symbol, bool isIsolated, OrderSide side, decimal takePrice, decimal quantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null,
            CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrderResponseBase> query = PreparePostTakeProfitMarketOrder(
                symbol, isIsolated, side, takePrice, quantity, id, sideEffect, orderResponseType))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for the iceberg order of the type <see cref="OrderType.Limit"/> with the specified order parameters.
        /// </summary>
        /// <param name="symbol">The symbol to post the order on.</param>
        /// <param name="isIsolated">The type of the margin account to post the order on (isolated or cross).</param>
        /// <param name="side">The order side.</param>
        /// <param name="tif">The time in force for the order.</param>
        /// <param name="price">The limit price to execute the order at.</param>
        /// <param name="totalQuantity">The total quantity of the base asset to trade.</param>
        /// <param name="partQuantity">The quantity of the base asset to trade with each iceberg part.</param>
        /// <param name="id">The custom identifier for the order (optional).</param>
        /// <param name="sideEffect">The margin action to take along with the order (optional).</param>
        /// <param name="orderResponseType">The type of response desired (optional).
        /// The default value is <see cref="OrderResponseType.Full"/>.</param>
        public IDeferredQuery<MarginOrderResponseBase> PreparePostIcebergLimitOrder(
            string symbol, bool isIsolated, OrderSide side, TimeInForce tif,
            decimal price, decimal totalQuantity, decimal partQuantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null)
        {
            return PreparePostOrder(
                symbol: symbol,
                isIsolated: isIsolated,
                side: side,
                type: OrderType.Limit,
                tif: tif,
                price: price,
                stopPrice: null,
                quantity: totalQuantity,
                quoteQuantity: null,
                icebergQuantity: partQuantity,
                id: id,
                sideEffect: sideEffect,
                orderResponseType: orderResponseType);
        }

        /// <summary>
        /// Posts a new iceberg order of the type <see cref="OrderType.Limit"/> with the specified order parameters.
        /// </summary>
        public Task<MarginOrderResponseBase> PostIcebergLimitOrderAsync(
            string symbol, bool isIsolated, OrderSide side, TimeInForce tif,
            decimal price, decimal totalQuantity, decimal partQuantity,
            string id = null, MarginSideEffect? sideEffect = null, OrderResponseType? orderResponseType = null,
            CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrderResponseBase> query = PreparePostIcebergLimitOrder(
                symbol, isIsolated, side, tif, price, totalQuantity, partQuantity, id, sideEffect, orderResponseType))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private IDeferredQuery<MarginOrderResponseBase> PreparePostOrder(
            string symbol, bool isIsolated, OrderSide side, OrderType type, TimeInForce? tif,
            decimal? price, decimal? stopPrice, decimal? quantity, decimal? quoteQuantity, decimal? icebergQuantity,
            string id, MarginSideEffect? sideEffect, OrderResponseType? orderResponseType)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));
            if (price <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(price));
            if (stopPrice <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(stopPrice));
            if (quantity <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(quantity));
            if (quoteQuantity <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(quoteQuantity));
            if (icebergQuantity <= 0.0m)
                throw new ArgumentOutOfRangeException(nameof(icebergQuantity));
            if (id != null && String.IsNullOrWhiteSpace(id))
                throw new ArgumentException("The specified custom ID is empty.", nameof(id));

            string relEndpoint = PostNewOrderEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.UID);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.UID), 6),
            };

            QueryBuilder qs = new QueryBuilder(374);
            qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            qs.AddParameter("isIsolated", isIsolated);
            qs.AddParameter("side", Format(side));
            qs.AddParameter("type", Format(type));
            if (tif != null)
            {
                qs.AddParameter("timeInForce", Format(tif.Value));
            }
            if (price != null)
            {
                qs.AddParameter("price", price.Value);
            }
            if (stopPrice != null)
            {
                qs.AddParameter("stopPrice", stopPrice.Value);
            }
            if (quantity != null)
            {
                qs.AddParameter("quantity", quantity.Value);
            }
            if (quoteQuantity != null)
            {
                qs.AddParameter("quoteOrderQty", quoteQuantity.Value);
            }
            if (icebergQuantity != null)
            {
                qs.AddParameter("icebergQty", icebergQuantity.Value);
            }
            if (id != null)
            {
                qs.AddParameter("newClientOrderId", id);
            }
            if (orderResponseType != null)
            {
                qs.AddParameter("newOrderRespType", Format(orderResponseType.Value));
            }
            if (sideEffect != null)
            {
                qs.AddParameter("sideEffectType", Format(sideEffect.Value));
            }

            OrderResponseType expectedRspType;
            if (orderResponseType != null)
            {
                expectedRspType = orderResponseType.Value;
            }
            else
            {
                expectedRspType = type == OrderType.Limit || type == OrderType.LimitMaker || type == OrderType.Market ?
                    OrderResponseType.Full : OrderResponseType.Ack;
            }

            return new DeferredQuery<MarginOrderResponseBase>(
                query: new QueryParams(HttpMethod.POST, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParsePostOrderResponse,
                parseArgs: expectedRspType,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        private MarginOrderResponseBase ParsePostOrderResponse(byte[] data, object parseArgs)
        {
            switch ((OrderResponseType)parseArgs)
            {
                case OrderResponseType.Ack:
                    return ParsePostOrderAckResponse(data);
                case OrderResponseType.Result:
                    return ParsePostOrderResultResponse(data);
                case OrderResponseType.Full:
                    return ParsePostOrderFullResponse(data);
                default:
                    throw new NotImplementedException($"The post order response type \"{parseArgs}\" is not implemented.");
            }
        }

        private MarginOrderResponseAck ParsePostOrderAckResponse(byte[] data)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);

            MarginOrderResponseAck response = new MarginOrderResponseAck();
            ParseSchemaValidator validator = new ParseSchemaValidator(4);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = reader.GetString();
                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);

                switch (propName)
                {
                    case "symbol":
                        response.Symbol = reader.GetString();
                        validator.RegisterProperty(0);
                        break;
                    case "orderId":
                        response.OrderId = reader.GetInt64();
                        validator.RegisterProperty(1);
                        break;
                    case "clientOrderId":
                        response.ClientOrderId = reader.GetString();
                        break;
                    case "isIsolated":
                        response.IsIsolated = reader.GetBoolean();
                        validator.RegisterProperty(2);
                        break;
                    case "transactTime":
                        response.TransactionTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(3);
                        break;
                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{propName}\" of the post order response was encountered.");
                        reader.Skip();
                        break;
                }
            }

            if (!validator.IsComplete())
            {
                const string objName = "post order response";
                int missingPropNum = validator.GetMissingPropertyNumber();
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "symbol");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "order ID");
                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "is isolated");
                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "transaction time");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            return response;
        }

        private MarginOrderResponseRes ParsePostOrderResultResponse(byte[] data)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);

            MarginOrderResponseRes response = new MarginOrderResponseRes();
            ParseSchemaValidator validator = new ParseSchemaValidator(8);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = reader.GetString();
                if (!reader.Read())
                    throw new JsonException($"A value of the property \"{propName}\" was expected " +
                        "but the end of the data was reached.");

                switch (propName)
                {
                    case "symbol":
                        response.Symbol = reader.GetString();
                        validator.RegisterProperty(0);
                        break;
                    case "orderId":
                        response.OrderId = reader.GetInt64();
                        validator.RegisterProperty(1);
                        break;
                    case "clientOrderId":
                        response.ClientOrderId = reader.GetString();
                        break;
                    case "transactTime":
                        response.TransactionTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(2);
                        break;
                    case "price":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal price);
                        response.Price = price;
                        break;
                    case "stopPrice":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal stopPrice);
                        response.StopPrice = stopPrice;
                        break;
                    case "origQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal origBaseQt);
                        response.OriginalBaseQuantity = origBaseQt;
                        break;
                    case "executedQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out response.ExecutedBaseQuantity);
                        break;
                    case "cummulativeQuoteQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal cumQuoteQt);
                        if (cumQuoteQt >= 0.0m) response.CummulativeQuoteQuantity = cumQuoteQt;
                        break;
                    case "status":
                        response.OrderStatus = ParseUtility.ParseOrderStatus(reader.GetString());
                        validator.RegisterProperty(3);
                        break;
                    case "timeInForce":
                        response.TimeInForce = ParseTimeInForce(reader.GetString());
                        validator.RegisterProperty(4);
                        break;
                    case "type":
                        response.OrderType = ParseOrderType(reader.GetString());
                        validator.RegisterProperty(5);
                        break;
                    case "isIsolated":
                        response.IsIsolated = reader.GetBoolean();
                        validator.RegisterProperty(6);
                        break;
                    case "side":
                        response.OrderSide = ParseUtility.ParseOrderSide(reader.GetString());
                        validator.RegisterProperty(7);
                        break;
                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{propName}\" of the post order response was encountered.");
                        reader.Skip();
                        break;
                }
            }

            if (!validator.IsComplete())
            {
                const string objName = "post order response";
                int missingPropNum = validator.GetMissingPropertyNumber();
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "symbol");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "order ID");
                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "transaction time");
                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "status");
                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "time in force");
                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "type");
                    case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "is isolated");
                    case 7: throw ParseUtility.GenerateMissingPropertyException(objName, "side");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            return response;
        }

        private MarginOrderResponseFull ParsePostOrderFullResponse(byte[] data)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);

            MarginOrderResponseFull response = new MarginOrderResponseFull();
            ParseSchemaValidator validator = new ParseSchemaValidator(8);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = reader.GetString();
                if (!reader.Read())
                    throw new JsonException($"A value of the property \"{propName}\" was expected " +
                        "but the end of the data was reached.");

                // Parse order response properties.
                switch (propName)
                {
                    case "symbol":
                        response.Symbol = reader.GetString();
                        validator.RegisterProperty(0);
                        break;
                    case "orderId":
                        response.OrderId = reader.GetInt64();
                        validator.RegisterProperty(1);
                        break;
                    case "clientOrderId":
                        response.ClientOrderId = reader.GetString();
                        break;
                    case "transactTime":
                        response.TransactionTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        validator.RegisterProperty(2);
                        break;
                    case "price":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal price);
                        response.Price = price;
                        break;
                    case "stopPrice":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal stopPrice);
                        response.StopPrice = stopPrice;
                        break;
                    case "origQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal origBaseQt);
                        response.OriginalBaseQuantity = origBaseQt;
                        break;
                    case "executedQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out response.ExecutedBaseQuantity);
                        break;
                    case "cummulativeQuoteQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal cumQuoteQt);
                        if (cumQuoteQt >= 0.0m) response.CummulativeQuoteQuantity = cumQuoteQt;
                        break;
                    case "status":
                        response.OrderStatus = ParseUtility.ParseOrderStatus(reader.GetString());
                        validator.RegisterProperty(3);
                        break;
                    case "timeInForce":
                        response.TimeInForce = ParseTimeInForce(reader.GetString());
                        validator.RegisterProperty(4);
                        break;
                    case "type":
                        response.OrderType = ParseOrderType(reader.GetString());
                        validator.RegisterProperty(5);
                        break;
                    case "side":
                        response.OrderSide = ParseUtility.ParseOrderSide(reader.GetString());
                        validator.RegisterProperty(6);
                        break;
                    case "marginBuyBorrowAmount":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal buyBorrowQt);
                        response.MarginBuyBorrowAmount = buyBorrowQt;
                        break;
                    case "marginBuyBorrowAsset":
                        response.MarginBuyBorrowAsset = reader.GetString();
                        break;
                    case "isIsolated":
                        response.IsIsolated = reader.GetBoolean();
                        validator.RegisterProperty(7);
                        break;
                    case "fills":
                        response.Fills = ParseUtility.ParseOrderPartialFills(ref reader);
                        break;
                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{propName}\" of the post order response was encountered.");
                        reader.Skip();
                        break;
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "post order response";
                int missingPropNum = validator.GetMissingPropertyNumber();
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "symbol");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "order ID");
                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "transaction time");
                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "status");
                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "time in force");
                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "type");
                    case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "side");
                    case 7: throw ParseUtility.GenerateMissingPropertyException(objName, "is isolated");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            // Return the result.
            return response;
        }

        // Cancel order.
        /// <summary>
        /// Prepares a query for cancellation of an active margin order with the specified unique identifier.
        /// </summary>
        /// <param name="symbol">The symbol that the order was placed on.</param>
        /// <param name="isIsolated">Indicates whether the the order was posted
        /// from the isolated margin account or the cross margin one.</param>
        /// <param name="orderId">A global numerical identifier of the order to cancel.</param>
        public IDeferredQuery<MarginOrder> PrepareCancelOrder(string symbol, bool isIsolated, long orderId)
        {
            if (String.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            return PrepareCancelOrder(symbol, isIsolated, orderId, null, null);
        }

        /// <summary>
        /// Cancels an active margin order asynchronously with the specified unique identifier.
        /// </summary>
        public Task<MarginOrder> CancelOrderAsync(string symbol, bool isIsolated, long orderId, CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrder> query = PrepareCancelOrder(symbol, isIsolated, orderId))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for cancellation of an active margin order with the specified custom identifier.
        /// </summary>
        /// <param name="symbol">The symbol that the order was placed on.</param>
        /// <param name="isIsolated">Indicates whether the the order was posted
        /// from the isolated margin account or the cross margin one.</param>
        /// <param name="clientOrderId">An original custom identifier of the order to cancel.</param>
        public IDeferredQuery<MarginOrder> PrepareCancelOrder(string symbol, bool isIsolated, string clientOrderId)
        {
            if (String.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));
            if (String.IsNullOrWhiteSpace(clientOrderId))
                throw new ArgumentNullException(nameof(clientOrderId));

            return PrepareCancelOrder(symbol, isIsolated, null, clientOrderId, null);
        }

        /// <summary>
        /// Cancels an active margin order asynchronously with the specified custom identifier.
        /// </summary>
        public Task<MarginOrder> CancelOrderAsync(string symbol, bool isIsolated, string clientOrderId, CancellationToken ct = default)
        {
            using (IDeferredQuery<MarginOrder> query = PrepareCancelOrder(symbol, isIsolated, clientOrderId))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private IDeferredQuery<MarginOrder> PrepareCancelOrder(string symbol, bool isIsolated,
            long? orderId = null, string origClientOrderId = null, string newClientOrderId = null)
        {
            ThrowIfNotRunning();

            string relEndpoint = DeleteOrderEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            QueryBuilder qs = new QueryBuilder(195);
            qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            qs.AddParameter("isIsolated", isIsolated);
            if (orderId != null)
            {
                qs.AddParameter("orderId", orderId.Value);
            }
            else
            {
                qs.AddParameter("origClientOrderId", origClientOrderId);
            }
            if (!String.IsNullOrEmpty(newClientOrderId))
            {
                qs.AddParameter("newClientOrderId", newClientOrderId);
            }

            return new DeferredQuery<MarginOrder>(
                query: new QueryParams(HttpMethod.DELETE, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseOrder,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        // Cancell all open orders.
        /// <summary>
        /// Prepares a query for cancellation of all active orders on a symbol, including OCO orders.
        /// </summary>
        /// <param name="symbol">The symbol to cancel orders on.</param>
        /// <param name="isIsolated">Indicates whether to cancel orders from the isolated margin account of the cross margin one.</param>
        public IDeferredQuery<List<MarginOrder>> PrepareCancelAllOrders(string symbol, bool isIsolated)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            string relEndpoint = DeleteAllOpenOrdersOnSymbolEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 1),
            };

            QueryBuilder qs = new QueryBuilder(155);
            qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            qs.AddParameter("isIsolated", isIsolated);

            return new DeferredQuery<List<MarginOrder>>(
                query: new QueryParams(HttpMethod.DELETE, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseOrderList,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Cancels all active orders on a symbol asynchronously, including OCO orders.
        /// </summary>
        public Task<List<MarginOrder>> CancelAllOrdersAsync(string symbol, bool isIsolated, CancellationToken ct = default)
        {
            using (IDeferredQuery<List<MarginOrder>> query = PrepareCancelAllOrders(symbol, isIsolated))
            {
                return query.ExecuteAsync(ct);
            }
        }

        // Get order.
        /// <summary>
        /// Prepares a query for info on a margin order (either active or historical) with the specified unique ID.
        /// </summary>
        /// <param name="symbol">The symbol that the order was placed on.</param>
        /// <param name="isIsolated">Indicates whether the the order was posted
        /// from the isolated margin account or the cross margin one.</param>
        /// <param name="orderId">The unique identifier of the order to fetch.</param>
        public IDeferredQuery<MarginOrder> PrepareGetOrder(string symbol, bool isIsolated, long orderId)
        {
            if (String.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            return PrepareGetOrder(symbol, isIsolated, orderId, null);
        }

        /// <summary>
        /// <summary>
        /// Prepares a query for info on a margin order (either active or historical) with the specified custom ID.
        /// </summary>
        /// <param name="symbol">The symbol that the order was placed on.</param>
        /// <param name="isIsolated">Indicates whether the the order was posted
        /// from the isolated margin account or the cross margin one.</param>
        /// <param name="clientOrderId">The original custom identifier of the order to fetch.</param>
        public IDeferredQuery<MarginOrder> PrepareGetOrder(string symbol, bool isIsolated, string clientOrderId)
        {
            if (String.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            return PrepareGetOrder(symbol, isIsolated, null, clientOrderId);
        }

        private IDeferredQuery<MarginOrder> PrepareGetOrder(string symbol, bool isIsolated,
            long? orderId = null, string origClientOrderId = null)
        {
            ThrowIfNotRunning();

            string relEndpoint = GetOrderEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            QueryBuilder qs = new QueryBuilder(195);
            qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            qs.AddParameter("isIsolated", isIsolated);
            if (orderId != null)
            {
                qs.AddParameter("orderId", orderId.Value);
            }
            else
            {
                qs.AddParameter("origClientOrderId", origClientOrderId);
            }

            return new DeferredQuery<MarginOrder>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseOrder,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        // Get all orders.
        /// <summary>
        /// Prepares a query for info on all margin orders; active, canceled, or filled.
        /// </summary>
        /// <param name="symbol">The symbol to list orders on.</param>
        /// <param name="isIsolated">Indicates whether to fetch orders for the isolated margin account or the cross margin one.</param>
        /// <param name="fromId">The start order ID to fetch from. If not specified, most recent orders will be sent.</param>
        /// <param name="startTime">The time to fetch orders from.</param>
        /// <param name="endTime">The time to fetch orders prior it.</param>
        /// <param name="limit">A maximum number of orders to fetch. The default value is 500, the maximum value is 500.</param>
        public IDeferredQuery<List<MarginOrder>> PrepareGetAllOrders(string symbol, bool isIsolated,
            long? fromId = null, DateTime? startTime = null, DateTime? endTime = null, int? limit = null)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));
            if (startTime != null && endTime != null)
            {
                TimeSpan diff = endTime.Value - startTime.Value;
                if (diff.Ticks < 0)
                    throw new ArgumentException($"The specifed time period [{startTime} ; {endTime}] is invalid.");
                else if (diff.Ticks > MaxAccountOrderLookupInterval)
                    throw new ArgumentException(
                        $"The duration of the specified time period [{startTime} ; {endTime}] exceeds the the limit " +
                        $"({new TimeSpan(MaxAccountOrderLookupInterval).TotalHours} hours). Check {nameof(MaxAccountOrderLookupInterval)}.");
            }
            if (limit < 1 || limit > 500)
                throw new ArgumentOutOfRangeException(nameof(limit));

            string relEndpoint = GetAllOrdersEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 200),
            };

            QueryBuilder qs = new QueryBuilder(193);
            qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            qs.AddParameter("isIsolated", isIsolated);
            if (fromId != null)
            {
                qs.AddParameter("orderId", fromId.Value);
            }
            if (startTime != null)
            {
                qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
            }
            if (endTime != null)
            {
                qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
            }
            if (limit != null)
            {
                qs.AddParameter("limit", limit.Value);
            }

            return new DeferredQuery<List<MarginOrder>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseOrderList,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets info on all margin orders asynchronously; active, canceled, or filled.
        /// </summary>
        public Task<List<MarginOrder>> GetAllOrdersAsync(
            string symbol, bool isIsolated, long? fromId = null,
            DateTime? startTime = null, DateTime? endTime = null, int? limit = null, CancellationToken ct = default)
        {
            using (IDeferredQuery<List<MarginOrder>> query = PrepareGetAllOrders(
                symbol, isIsolated, fromId, startTime, endTime, limit))
            {
                return query.ExecuteAsync(ct);
            }
        }

        // Get all open orders.
        /// <summary>
        /// Prepares a query for info on all open margin orders.
        /// </summary>
        /// /// <param name="isIsolated">Indicates whether to fetch orders for the isolated margin account or the cross margin one.</param>
        /// <param name="symbol">
        /// The symbol to list open orders on.
        /// <para>If <paramref name="isIsolated"/> is <see langword="false"/> the value can be <c>Null</c>, otherwise it must be specified.</para>
        /// </param>
        public IDeferredQuery<List<MarginOrder>> PrepareGetOpenOrders(bool isIsolated, string symbol)
        {
            ThrowIfNotRunning();
            if (isIsolated)
            {
                if (String.IsNullOrWhiteSpace(symbol))
                    throw new ArgumentNullException(nameof(symbol));
            }
            else
            {
                if (String.IsNullOrEmpty(symbol))
                    symbol = null;
            }
            
            string relEndpoint = GetOpenOrdersEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(
                    GetWeightDimensionId(relEndpoint, RateLimitType.IP),
                    (uint)(symbol != null ? 10 : 10 * ExpectedCrossPairsCount)),
            };

            QueryBuilder qs = new QueryBuilder(137);
            if (symbol != null) qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            qs.AddParameter("isIsolated", isIsolated);

            return new DeferredQuery<List<MarginOrder>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseOrderList,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        /// <summary>
        /// Gets info on all open margin orders asynchronously.
        /// </summary>
        public Task<List<MarginOrder>> GetOpenOrdersAsync(bool isIsolated, string symbol, CancellationToken ct = default)
        {
            using (IDeferredQuery<List<MarginOrder>> query = PrepareGetOpenOrders(isIsolated, symbol))
            {
                return query.ExecuteAsync(ct);
            }
        }

        // Get account trades.
        /// <summary>
        /// Prepares a query for margin account's trades executed within a single order.
        /// </summary>
        /// <param name="symbol">The symbol to list trades on.</param>
        /// <param name="isIsolated">Indicates whether to fetch trades for the isolated margin account or the cross margin one.</param>
        /// <param name="orderId">The unique identifier of the order to get trades for.</param>
        public IDeferredQuery<List<MarginTrade>> PrepareGetAccountTrades(string symbol, bool isIsolated, long orderId)
        {
            return PrepareGetAccountTrades(symbol, isIsolated, orderId, null, null, null, null);
        }

        /// <summary>
        /// Gets margin account's trades executed within a single order asynchronously.
        /// </summary>
        public Task<List<MarginTrade>> GetAccountTradesAsync(string symbol, bool isIsolated, long orderId, CancellationToken ct = default)
        {
            using (IDeferredQuery<List<MarginTrade>> query = PrepareGetAccountTrades(symbol, isIsolated, orderId))
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for margin account's trades.
        /// </summary>
        /// <param name="symbol">The symbol to list trades on.</param>
        /// <param name="isIsolated">Indicates whether to fetch trades for the isolated margin account or the cross margin one.</param>
        /// <param name="startTime">The time to get trades from (inclusive).</param>
        /// <param name="endTime">The time to get trades prior to (inclusive).
        /// <para>If both <paramref name="startTime"/> and <paramref name="endTime"/> are specified, 
        /// the period between them must not be longer than <see cref="MaxAccountTradeLookupInterval"/> (24 hours).</para></param>
        /// <param name="fromId">The trade identifier to fetch data from (inclusive). Cannot be used in combination with <paramref name="limit"/>.</param>
        /// <param name="limit">
        /// A maximum number of trades to fetch.
        /// <para>The default value is 500. The maximum value is 1000.</para>
        /// <para>Cannot be used in combintation with <paramref name="fromId"/>.</para>
        /// </param>
        public IDeferredQuery<List<MarginTrade>> PrepareGetAccountTrades(string symbol, bool isIsolated,
            DateTime? startTime = null, DateTime? endTime = null, long? fromId = null, int? limit = null)
        {
            return PrepareGetAccountTrades(symbol, isIsolated, null, startTime, endTime, fromId, limit);
        }

        /// <summary>
        /// Gets margin account's trades asynchronously.
        /// </summary>
        public Task<List<MarginTrade>> GetAccountTradesAsync(
            string symbol, bool isIsolated, DateTime? startTime = null, DateTime? endTime = null,
            long? fromId = null, int? limit = null, CancellationToken ct = default)
        {
            using (IDeferredQuery<List<MarginTrade>> query = PrepareGetAccountTrades(
                symbol, isIsolated, startTime, endTime, fromId, limit))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private IDeferredQuery<List<MarginTrade>> PrepareGetAccountTrades(string symbol, bool isIsolated,
            long? orderId = null, DateTime? startTime = null, DateTime? endTime = null, long? fromId = null, int? limit = null)
        {
            ThrowIfNotRunning();
            if (String.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));
            if (orderId < 0)
                throw new ArgumentOutOfRangeException(nameof(orderId));
            if (startTime != null && endTime != null)
            {
                TimeSpan diff = endTime.Value - startTime.Value;
                if (diff.Ticks < 0)
                    throw new ArgumentException($"The specifed time period [{startTime} ; {endTime}] is invalid.");
                else if (diff.Ticks > MaxAccountTradeLookupInterval)
                    throw new ArgumentException(
                        $"The duration of the specified time period [{startTime} ; {endTime}] exceeds the the limit " +
                        $"({new TimeSpan(MaxAccountTradeLookupInterval).TotalDays} days). Check {nameof(MaxAccountTradeLookupInterval)}.");
            }
            if (fromId < 0)
                throw new ArgumentOutOfRangeException(nameof(fromId));
            if (limit < 1 || limit > 1000)
                throw new ArgumentOutOfRangeException(nameof(limit));

            string relEndpoint = GetAccountTradeListEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 10),
            };

            QueryBuilder qs = new QueryBuilder(193);
            qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            qs.AddParameter("isIsolated", isIsolated);
            if (orderId != null)
            {
                qs.AddParameter("orderId", orderId.Value);
            }
            if (startTime != null)
            {
                qs.AddParameter("startTime", CommonUtility.ConvertToApiTime(startTime.Value));
            }
            if (endTime != null)
            {
                qs.AddParameter("endTime", CommonUtility.ConvertToApiTime(endTime.Value));
            }
            if (fromId != null)
            {
                qs.AddParameter("fromId", fromId.Value);
            }
            if (limit != null)
            {
                qs.AddParameter("limit", limit.Value);
            }

            return new DeferredQuery<List<MarginTrade>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseAccountTradeList,
                parseArgs: orderId != null ? 20 : (limit != null ? limit : 500),
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        private List<MarginTrade> ParseAccountTradeList(byte[] data, object parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadArrayStart(ref reader);

            List<MarginTrade> results = new List<MarginTrade>(parseArgs is int expectedCount ? expectedCount : 500);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ParseUtility.ValidateObjectStartToken(ref reader);
                MarginTrade trade = default;
                ParseSchemaValidator validator = new ParseSchemaValidator(12);

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string propName = reader.GetString();
                    if (!reader.Read())
                        throw ParseUtility.GenerateNoPropertyValueException(propName);

                    switch (propName)
                    {
                        case "commission":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out trade.Commission);
                            validator.RegisterProperty(0);
                            break;
                        case "commissionAsset":
                            trade.CommissionAsset = reader.GetString();
                            validator.RegisterProperty(1);
                            break;
                        case "id":
                            trade.Id = reader.GetInt64();
                            validator.RegisterProperty(2);
                            break;
                        case "isBestMatch":
                            trade.IsBestMatch = reader.GetBoolean();
                            break;
                        case "isBuyer":
                            trade.IsBuyer = reader.GetBoolean();
                            validator.RegisterProperty(3);
                            break;
                        case "isMaker":
                            trade.IsMaker = reader.GetBoolean();
                            validator.RegisterProperty(4);
                            break;
                        case "orderId":
                            trade.OrderId = reader.GetInt64();
                            validator.RegisterProperty(5);
                            break;
                        case "price":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out trade.Price);
                            validator.RegisterProperty(6);
                            break;
                        case "qty":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out trade.Quantity);
                            validator.RegisterProperty(7);
                            break;
                        case "quoteQty":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out trade.QuoteQuantity);
                            validator.RegisterProperty(8);
                            break;
                        case "symbol":
                            trade.Symbol = reader.GetString();
                            validator.RegisterProperty(9);
                            break;
                        case "isIsolated":
                            trade.IsIsolated = reader.GetBoolean();
                            validator.RegisterProperty(10);
                            break;
                        case "time":
                            trade.Time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                            validator.RegisterProperty(11);
                            break;
                        default:
                            throw ParseUtility.GenerateUnknownPropertyException(propName);
                    }
                }

                if (!validator.IsComplete())
                {
                    const string objName = "margin account trade";
                    int missingPropNum = validator.GetMissingPropertyNumber();
                    switch (missingPropNum)
                    {
                        case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "commission size");
                        case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "commission asset");
                        case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "ID");
                        case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "is buyer");
                        case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "is maker");
                        case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "order ID");
                        case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "price");
                        case 7: throw ParseUtility.GenerateMissingPropertyException(objName, "quantity");
                        case 8: throw ParseUtility.GenerateMissingPropertyException(objName, "quote quantity");
                        case 9: throw ParseUtility.GenerateMissingPropertyException(objName, "symbol");
                        case 10: throw ParseUtility.GenerateMissingPropertyException(objName, "is isolated");
                        case 11: throw ParseUtility.GenerateMissingPropertyException(objName, "time");
                        default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                    }
                }

                results.Add(trade);
                validator.Reset();
            }

            return results;
        }

        // Get current order limit usage.
        /// <summary>
        /// Prepares a query for a user's current cross margin order count usage for all limit intervals.
        /// </summary>
        public IDeferredQuery<List<RateLimiter>> PrepareGetOrderLimitUsage()
        {
            return PrepareGetOrderLimitUsage(false, null);
        }

        /// <summary>
        /// Gets a user's current cross margin order count usage for all limit intervals asynchrnously.
        /// </summary>
        public Task<List<RateLimiter>> GetOrderLimitUsageAsync(CancellationToken ct)
        {
            using (IDeferredQuery<List<RateLimiter>> query = PrepareGetOrderLimitUsage())
            {
                return query.ExecuteAsync(ct);
            }
        }

        /// <summary>
        /// Prepares a query for a user's current isolated margin order count usage for all limit intervals.
        /// </summary>
        /// <param name="isolatedSymbol">The symbol associated with the isolated margin account to get limit usage for.</param>
        public IDeferredQuery<List<RateLimiter>> PrepareGetOrderLimitUsage(string isolatedSymbol)
        {
            if (String.IsNullOrWhiteSpace(isolatedSymbol))
                throw new ArgumentNullException(nameof(isolatedSymbol));
            return PrepareGetOrderLimitUsage(true, isolatedSymbol);
        }

        /// <summary>
        /// Gets a user's current isolated margin order count usage for all limit intervals asynchrnously.
        /// </summary>
        public Task<List<RateLimiter>> GetOrderLimitUsageAsync(string isolatedSymbol, CancellationToken ct)
        {
            using (IDeferredQuery<List<RateLimiter>> query = PrepareGetOrderLimitUsage(isolatedSymbol))
            {
                return query.ExecuteAsync(ct);
            }
        }

        private IDeferredQuery<List<RateLimiter>> PrepareGetOrderLimitUsage(bool isIsolated, string symbol)
        {
            ThrowIfNotRunning();

            string relEndpoint = GetOrderLimitUsageEndpoint;
            RegisterRateLimitsIfNotExist(relEndpoint, RateLimitType.IP);

            QueryWeight[] weights = new QueryWeight[]
            {
                new QueryWeight(GetWeightDimensionId(relEndpoint, RateLimitType.IP), 20),
            };

            QueryBuilder qs = new QueryBuilder(154);
            qs.AddParameter("isIsolated", isIsolated);
            if (symbol != null)
            {
                qs.AddParameter("symbol", CommonUtility.NormalizeSymbol(symbol));
            }

            return new DeferredQuery<List<RateLimiter>>(
                query: new QueryParams(HttpMethod.GET, RESTEndpoint.Url, relEndpoint, qs, true),
                executeHandler: ExecuteQueryAsync,
                parseHandler: ParseRateLimiters,
                weights: weights,
                headersToLimitsMap: GetHeadersToLimitsMap(relEndpoint));
        }

        private List<RateLimiter> ParseRateLimiters(byte[] data, object parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ParseUtility.ReadArrayStart(ref reader);
            List<RateLimiter> limiters = new List<RateLimiter>(6);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                limiters.Add(ParseRateLimiter(ref reader));
            }

            return limiters;
        }

        // Trading pairs queries logic.
        private List<MarginPair> ParseTradingPairList(byte[] data, object parseArgs)
        {
            bool isIsolated = (bool)parseArgs;
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadArrayStart(ref reader);

            List<MarginPair> resultList = new List<MarginPair>(isIsolated ? ExpectedIsolatedPairsCount : ExpectedCrossPairsCount);
            List<JsonException> parseErrors = null;

            // Parse each margin pair.
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ParseUtility.ValidateObjectStartToken(ref reader);
                int objDepth = reader.CurrentDepth;

                try
                {
                    resultList.Add(ParseTradingPair(ref reader, isIsolated));
                }
                catch (JsonException jExc)
                {
                    ParseUtility.SkipTillObjectEnd(ref reader, objDepth);
                    if (parseErrors == null)
                        parseErrors = new List<JsonException>();
                    parseErrors.Add(jExc);
                }
            }

            // Check whether any parse errors occurred.
            if (parseErrors != null)
            {
                if (resultList.Count == 0)
                {
                    JsonException innerExc = parseErrors[0];
                    throw new JsonException(
                        $"Parsing all of the {parseErrors.Count} margin pairs failed: {innerExc.Message}", innerExc);
                }
                else
                {
                    foreach (JsonException jExc in parseErrors)
                    {
                        PostLogMessage(LogLevel.Warning, $"Parsing a margin pair failed:\r\n{jExc}");
                    }
                }
            }

            // Return the result.
            return resultList;
        }

        private MarginPair ParseTradingPair(ref Utf8JsonReader reader, bool isIsolated)
        {
            ParseUtility.ValidateObjectStartToken(ref reader);

            MarginPair pair = default;
            ParseSchemaValidator validator = new ParseSchemaValidator(4);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                
                switch (propName)
                {
                    case "base":
                        pair.BaseAsset = reader.GetString();
                        validator.RegisterProperty(0);
                        break;
                    case "id":
                        pair.Id = reader.GetInt64();
                        validator.RegisterProperty(1);
                        break;
                    case "isBuyAllowed":
                        pair.IsBuyAllowed = reader.GetBoolean();
                        break;
                    case "isMarginTrade":
                        pair.IsMarginTrade = reader.GetBoolean();
                        break;
                    case "isSellAllowed":
                        pair.IsSellAllowed = reader.GetBoolean();
                        break;
                    case "quote":
                        pair.QuoteAsset = reader.GetString();
                        validator.RegisterProperty(2);
                        break;
                    case "symbol":
                        pair.Symbol = reader.GetString();
                        validator.RegisterProperty(3);
                        break;
                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown property \"{propName}\" of the " + 
                            (isIsolated ? "isolated" : "cross") + " margin pair was encountered.");
                        reader.Skip();
                        break;
                }
            }

            // The ID property is not present in isolated pairs.
            if (isIsolated)
            {
                pair.Id = -1;
                validator.RegisterProperty(1); 
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                string objName = isIsolated ? "isolated margin pair" : "cross margin pair";
                int missingPropNum = validator.GetMissingPropertyNumber();
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "base asset");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "ID");
                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "quote asset");
                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "symbol");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            // Return the result.
            return pair;
        }

        // Transaction queries logic.
        private void ValidateTransactionHistoryInterval(ref DateTime? startTime, ref DateTime? endTime)
        {
            if (startTime != null && endTime != null)
            {
                TimeSpan diff = endTime.Value - startTime.Value;
                if (diff.Ticks < 0)
                    throw new ArgumentException($"The specifed time period [{startTime} ; {endTime}] is invalid.");
                else if (diff.Ticks > MaxTransactionLookupInterval)
                    throw new ArgumentException(
                        $"The duration of the specified time period [{startTime} ; {endTime}] exceeds the the limit " +
                        $"({new TimeSpan(MaxTransactionLookupInterval).TotalDays} days). Check {nameof(MaxTransactionLookupInterval)}.");
            }
        }

        private long ParseTransactionId(byte[] data, object parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);

            long? id = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw ParseUtility.GenerateNoPropertyValueException(propName);
                switch (propName)
                {
                    case "tranId":
                        id = reader.GetInt64();
                        break;
                    case "clientTag":
                        // The property is not stored.
                        break;
                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown transaction response property \"{propName}\" was encountered.");
                        reader.Skip();
                        break;
                }
            }

            return id ?? throw ParseUtility.GenerateMissingPropertyException("transaction response", "ID");
        }

        // Orders parse logic.
        public List<MarginOrder> ParseOrderList(byte[] data, object parseArgs)
        {
            List<MarginOrder> orders = new List<MarginOrder>(parseArgs is int expectedCount ? expectedCount : 32);
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);

            ParseUtility.ReadArrayStart(ref reader);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                orders.Add(ParseOrder(ref reader));
            }
            return orders;
        }

        public MarginOrder ParseOrder(byte[] data, object parseArgs)
        {
            Utf8JsonReader reader = new Utf8JsonReader(data, ParseUtility.ReaderOptions);
            ParseUtility.ReadObjectStart(ref reader);
            return ParseOrder(ref reader);
        }

        private MarginOrder ParseOrder(ref Utf8JsonReader reader)
        {
            ParseUtility.ValidateObjectStartToken(ref reader);

            MarginOrder order = new MarginOrder();
            ParseSchemaValidator validator = new ParseSchemaValidator(8);

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ParseUtility.ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw new JsonException($"A value of the property \"{propName}\" was expected but the end of the data was reached.");
                switch (propName)
                {
                    case "clientOrderId":
                        order.ClientOrderId = reader.GetString();
                        break;
                    case "origClientOrderId":
                        // This property is not stored. It's only encountered in cancel order responses.
                        reader.Skip();
                        break;
                    case "cummulativeQuoteQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal cumQuoteQt);
                        if (cumQuoteQt >= 0.0m) order.CummulativeQuoteQuantity = cumQuoteQt;
                        break;
                    case "executedQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out order.ExecutedBaseQuantity);
                        validator.RegisterProperty(0);
                        break;
                    case "icebergQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal icebergQt);
                        order.IcebergQuantity = icebergQt;
                        break;
                    case "isWorking":
                        order.IsWorking = reader.GetBoolean();
                        break;
                    case "orderId":
                        if (reader.TokenType == JsonTokenType.Number)
                            order.OrderId = reader.GetInt64();
                        else if (reader.TokenType == JsonTokenType.String)
                            order.OrderId = long.Parse(reader.GetString());
                        else
                            throw ParseUtility.GenerateInvalidValueTypeException(propName, JsonTokenType.Number, reader.TokenType);
                        validator.RegisterProperty(1);
                        break;
                    case "origQty":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal origQt);
                        order.OriginalBaseQuantity = origQt;
                        break;
                    case "price":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal price);
                        order.Price = price;
                        break;
                    case "side":
                        order.Side = ParseUtility.ParseOrderSide(reader.GetString());
                        validator.RegisterProperty(2);
                        break;
                    case "status":
                        order.Status = ParseUtility.ParseOrderStatus(reader.GetString());
                        validator.RegisterProperty(3);
                        break;
                    case "stopPrice":
                        ParseUtility.ParseDecimal(propName, reader.GetString(), out decimal stopPrice);
                        order.StopPrice = stopPrice;
                        break;
                    case "symbol":
                        order.Symbol = reader.GetString();
                        validator.RegisterProperty(4);
                        break;
                    case "isIsolated":
                        order.IsIsolated = reader.GetBoolean();
                        validator.RegisterProperty(5);
                        break;
                    case "time":
                        order.Time = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        break;
                    case "timeInForce":
                        order.TimeInForce = ParseTimeInForce(reader.GetString());
                        validator.RegisterProperty(6);
                        break;
                    case "type":
                        order.Type = ParseOrderType(reader.GetString());
                        validator.RegisterProperty(7);
                        break;
                    case "updateTime":
                        order.UpdateTime = CommonUtility.ConvertToDateTime(reader.GetInt64());
                        break;
                    case "accountId":
                        order.AccountId = reader.GetInt64();
                        break;
                    default:
                        PostLogMessage(
                            LogLevel.Warning,
                            $"An unknown order property \"{propName}\" was encountered.");
                        reader.Skip();
                        break;
                }
            }

            if (!validator.IsComplete())
            {
                const string objName = "order";
                int missingPropNum = validator.GetMissingPropertyNumber();
                switch (missingPropNum)
                {
                    case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "executed quantity");
                    case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "order ID");
                    case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "side");
                    case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "status");
                    case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "symbol");
                    case 5: throw ParseUtility.GenerateMissingPropertyException(objName, "is isolated");
                    case 6: throw ParseUtility.GenerateMissingPropertyException(objName, "time in force");
                    case 7: throw ParseUtility.GenerateMissingPropertyException(objName, "type");
                    default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                }
            }

            return order;
        }

        #endregion
    }
}
