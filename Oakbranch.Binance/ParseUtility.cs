using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Oakbranch.Binance.Filters.Exchange;
using Oakbranch.Binance.Filters.Symbol;

namespace Oakbranch.Binance
{
    internal static class ParseUtility
    {
        #region Static members

        public static JsonReaderOptions ReaderOptions { get; } = new JsonReaderOptions();

        #endregion

        #region Static methods

        // JSON reading.
        public static void ReadObjectStart(ref Utf8JsonReader reader)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"An object start was expected but encountered \"{reader.TokenType}\".");
        }

        public static void ReadObjectEnd(ref Utf8JsonReader reader)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException($"An object end was expected but encountered \"{reader.TokenType}\".");
        }

        public static void ValidateObjectStartToken(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"An object start was expected but encountered \"{reader.TokenType}\".");
        }

        public static void ReadArrayStart(ref Utf8JsonReader reader)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"An array's start was expected but encountered \"{reader.TokenType}\".");
        }

        public static void ReadArrayEnd(ref Utf8JsonReader reader)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException($"An array's end was expected but encountered \"{reader.TokenType}\".");
        }

        public static void ValidateArrayStartToken(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"An array's start was expected but encountered \"{reader.TokenType}\".");
        }

        /// <summary>
        /// Reads the next JSON property name from the specified JSON reader and returns it as a string.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <returns>The property name read.</returns>
        /// <exception cref="JsonException">Thrown when the reader encounters a token that is not a property name.</exception>
        public static string ReadPropertyName(ref Utf8JsonReader reader)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"A property's name was expected but \"{reader.TokenType}\" encountered.");
            return reader.GetString();
        }

        /// <summary>
        /// Reads the next JSON property name from the specified JSON reader and ensures that it matches the specified name.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="propName">The expected name of the property.</param>
        /// <exception cref="JsonException">Thrown when the reader encounters a token that is not a property name, or when the property name
        /// read from the reader does not match the expected property name.</exception>
        public static void ReadExactPropertyName(ref Utf8JsonReader reader, string propName)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"A propertie's name was expected but \"{reader.TokenType}\" encountered.");
            if (reader.GetString() != propName)
                throw new JsonException($"The property \"{propName}\" was expected but \"{reader.GetString()}\" encountered.");
        }

        public static void ValidatePropertyNameToken(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"A propertie's name was expected but \"{reader.TokenType}\" encountered.");
        }

        public static void ValidatePropertyValueToken(ref Utf8JsonReader reader, JsonTokenType requiredToken, string propName)
        {
            if (!reader.Read() || reader.TokenType != requiredToken)
                throw GenerateInvalidValueTypeException(propName, requiredToken, reader.TokenType);
        }

        /// <summary>
        /// Skips over all the JSON elements until <see cref="JsonTokenType.EndObject"/> at the specified depth is encountered.
        /// </summary>
        /// <param name="reader">The JSON reader to skip elements from.</param>
        /// <param name="objectDepth">The depth of the object to skip until its end is reached.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="JsonException"/>
        public static void SkipTillObjectEnd(ref Utf8JsonReader reader, int objectDepth)
        {
            if (reader.CurrentDepth < objectDepth)
            {
                throw new ArgumentException(
                    $"The current depth of the JSON reader is already less ({reader.CurrentDepth})" +
                    $"than the specified depth of an object ({objectDepth}).");
            }

            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth <= objectDepth) return;

            while (reader.Read() && (reader.TokenType != JsonTokenType.EndObject || reader.CurrentDepth > objectDepth))
            {
                if (reader.CurrentDepth < objectDepth)
                {
                    throw new JsonException(
                        $"The JSON reader fell out of the specified depth ({objectDepth}) " +
                        $"without running into the {JsonTokenType.EndObject} token.");
                }
            }
        }

        /// <summary>
        /// Skips over all the JSON elements until <see cref="JsonTokenType.EndArray"/> at the specified depth is encountered.
        /// </summary>
        /// <param name="reader">The JSON reader to skip elements from.</param>
        /// <param name="arrayDepth">The depth of the array to skip until its end is reached.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="JsonException"/>
        public static void SkipTillArrayEnd(ref Utf8JsonReader reader, int arrayDepth)
        {
            if (reader.CurrentDepth < arrayDepth)
            {
                throw new ArgumentException(
                    $"The current depth of the JSON reader is already less ({reader.CurrentDepth})" +
                    $"than the specified depth of an object ({arrayDepth}).");
            }

            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth <= arrayDepth) return;

            while (reader.Read() && (reader.TokenType != JsonTokenType.EndObject || reader.CurrentDepth > arrayDepth))
            {
                if (reader.CurrentDepth < arrayDepth)
                {
                    throw new ArgumentException(
                        $"The JSON reader fell out of the specified depth ({arrayDepth}) " +
                        $"without running into the {JsonTokenType.EndArray} token.");
                }
            }
        }

        // Exceptions.
        public static JsonException GenerateUnknownPropertyException(string propName)
        {
            return new JsonException($"An unknown property was encountered: \"{propName}\".");
        }

        public static JsonException GenerateNoPropertyValueException(string propName)
        {
            return new JsonException($"A value of the property \"{propName}\" was expected but the end of the data was reached.");
        }

        public static JsonException GenerateInvalidValueTypeException(string propName,
            JsonTokenType expectedType, JsonTokenType actualType)
        {
            return new JsonException(
                $"The {propName} value of the type \"{expectedType}\" was expected but \"{actualType}\" encountered.");
        }

        public static JsonException GenerateMissingPropertyException(string objName, string propName)
        {
            return new JsonException($"The {propName} property of the {objName} object is missing in the response.");
        }

        // Shared data structures.
        public static ApiErrorInfo ParseErrorInfo(ReadOnlySpan<byte> input)
        {
            Utf8JsonReader reader = new Utf8JsonReader(input, ReaderOptions);
            ReadObjectStart(ref reader);

            if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("A property name is missing.");

            if (reader.GetString() != "code")
                throw new JsonException($"The code property was expected but \"{reader.GetString()}\" encountered.");

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"A value of the code was expected but \"{reader.TokenType}\" encountered.");

            int code = reader.GetInt32();

            if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("A property name is missing.");

            if (reader.GetString() != "msg")
                throw new JsonException($"The msg property was expected but \"{reader.GetString()}\" encountered.");

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException($"A value of the message was expected but \"{reader.TokenType}\" encountered.");

            string message = reader.GetString();

            return new ApiErrorInfo(code, message);
        }

        public static bool TryParseErrorInfo(ReadOnlySpan<byte> input, out ApiErrorInfo value)
        {
            try
            {
                value = ParseErrorInfo(input);
                return true;
            }
            catch (JsonException)
            {
                value = default;
                return false;
            }
        }

        public static OrderStatus ParseOrderStatus(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException("The order status value is null.");

            switch (s)
            {
                case "NEW":
                    return OrderStatus.New;
                case "PARTIALLY_FILLED":
                    return OrderStatus.PartiallyFilled;
                case "FILLED":
                    return OrderStatus.Filled;
                case "CANCELED":
                    return OrderStatus.Canceled;
                case "PENDING_CANCEL":
                    throw new NotImplementedException($"The order status \"s\" is not implemented for it was considered unused.");
                case "REJECTED":
                    return OrderStatus.Rejected;
                case "EXPIRED":
                    return OrderStatus.Expired;
                case "EXPIRED_IN_MATCH":
                    return OrderStatus.ExpiredInMatch;
                default:
                    throw new JsonException($"An unknown order status \"{s}\" was encountered.");
            }
        }

        public static OrderSide ParseOrderSide(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new JsonException($"The order side value is null.");

            switch (s)
            {
                case "BUY":
                    return OrderSide.Buy;
                case "SELL":
                    return OrderSide.Sell;
                default:
                    throw new JsonException($"An unknown order side \"{s}\" was encountered.");
            }
        }

        public static List<OrderPartialFill> ParseOrderPartialFills(ref Utf8JsonReader reader)
        {
            ParseUtility.ValidateArrayStartToken(ref reader);
            List<OrderPartialFill> resultsList = new List<OrderPartialFill>(8);

            ParseSchemaValidator validator = new ParseSchemaValidator(5);
            decimal price = 0.0m, qty = 0.0m, comm = 0.0m;
            long id = 0;
            string commAsset = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ParseUtility.ValidateObjectStartToken(ref reader);

                // Parse partial fill properties.
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    ParseUtility.ValidatePropertyNameToken(ref reader);
                    string propName = reader.GetString();
                    if (!reader.Read())
                        throw new JsonException($"A value of the property \"{propName}\" was expected but \"{reader.TokenType}\" encountered.");
                    switch (propName)
                    {
                        case "price":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out price);
                            validator.RegisterProperty(0);
                            break;
                        case "qty":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out qty);
                            validator.RegisterProperty(1);
                            break;
                        case "commission":
                            ParseUtility.ParseDecimal(propName, reader.GetString(), out comm);
                            validator.RegisterProperty(2);
                            break;
                        case "commissionAsset":
                            commAsset = reader.GetString();
                            validator.RegisterProperty(3);
                            break;
                        case "tradeId":
                            id = reader.GetInt64();
                            validator.RegisterProperty(4);
                            break;
                        default:
                            throw ParseUtility.GenerateUnknownPropertyException(propName);
                    }
                }

                // Check whether all the essential properties were provided.
                if (!validator.IsComplete())
                {
                    const string objName = "order partial fill";
                    int missingPropNum = validator.GetMissingPropertyNumber();
                    switch (missingPropNum)
                    {
                        case 0: throw ParseUtility.GenerateMissingPropertyException(objName, "price");
                        case 1: throw ParseUtility.GenerateMissingPropertyException(objName, "quantity");
                        case 2: throw ParseUtility.GenerateMissingPropertyException(objName, "commission");
                        case 3: throw ParseUtility.GenerateMissingPropertyException(objName, "commission asset");
                        case 4: throw ParseUtility.GenerateMissingPropertyException(objName, "trade ID");
                        default: throw ParseUtility.GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})");
                    }
                }

                // Add the partial fill to the results list.
                resultsList.Add(new OrderPartialFill(id, price, qty, comm, commAsset));
                validator.Reset();
            }

            return resultsList;
        }

        // Exchange filters.
        public static ExchangeFilter ParseExchangeFilter(ref Utf8JsonReader reader)
        {
            ValidateObjectStartToken(ref reader);

            const string propName = "filterType";
            ReadExactPropertyName(ref reader, propName);
            ValidatePropertyValueToken(ref reader, JsonTokenType.String, propName);
            string type = reader.GetString();

            switch (type)
            {
                case "EXCHANGE_MAX_NUM_ORDERS":
                    return ParseTotalOpenOrdersFilter(ref reader);
                case "EXCHANGE_MAX_ALGO_ORDERS":
                    return ParseTotalAlgoOrdersFilter(ref reader);
                default:
                    throw new JsonException($"An unknown exchange filter type was encountered: \"{type}\".");
            }
        }

        private static TotalOpenOrdersFilter ParseTotalOpenOrdersFilter(ref Utf8JsonReader reader)
        {
            TotalOpenOrdersFilter result = new TotalOpenOrdersFilter();

            const string propName = "maxNumOrders";
            ReadExactPropertyName(ref reader, propName);
            ValidatePropertyValueToken(ref reader, JsonTokenType.Number, propName);
            result.Limit = reader.GetUInt32();

            ReadObjectEnd(ref reader);

            return result;
        }

        private static TotalAlgoOrdersFilter ParseTotalAlgoOrdersFilter(ref Utf8JsonReader reader)
        {
            TotalAlgoOrdersFilter result = new TotalAlgoOrdersFilter();

            const string propName = "maxNumAlgoOrders";
            ReadExactPropertyName(ref reader, propName);
            ValidatePropertyValueToken(ref reader, JsonTokenType.Number, propName);
            result.Limit = reader.GetUInt32();

            ReadObjectEnd(ref reader);

            return result;
        }

        // Symbol filters.
        public static SymbolFilter ParseSymbolFilter(ref Utf8JsonReader reader)
        {
            ValidateObjectStartToken(ref reader);

            List<KeyValuePair<string, object>> objectProps = new List<KeyValuePair<string, object>>(5);
            string type = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                ValidatePropertyNameToken(ref reader);
                string propName = reader.GetString();

                if (!reader.Read())
                    throw GenerateNoPropertyValueException(propName);

                if (propName == "filterType")
                {
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException(
                            $"A symbol filter cannot be parsed: the type property has an invalid value token ({reader.TokenType}).");
                    }

                    type = reader.GetString();
                    if (String.IsNullOrWhiteSpace(type))
                    {
                        throw new JsonException(
                              $"A symbol filter cannot be parsed: the value of the type property is null.");
                    }

                    continue;
                }

                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        objectProps.Add(new KeyValuePair<string, object>(propName, reader.GetString()));
                        break;
                    case JsonTokenType.Number:
                        objectProps.Add(new KeyValuePair<string, object>(propName, reader.GetInt64()));
                        break;
                    case JsonTokenType.True:
                        objectProps.Add(new KeyValuePair<string, object>(propName, true));
                        break;
                    case JsonTokenType.False:
                        objectProps.Add(new KeyValuePair<string, object>(propName, false));
                        break;
                    case JsonTokenType.Null:
                        objectProps.Add(new KeyValuePair<string, object>(propName, null));
                        break;
                    default:
                        throw new JsonException(
                            $"The token \"{reader.TokenType}\" is not a valid value token" +
                            $"for the symbol filter property \"{propName}\".");
                }
            }

            if (type == null)
            {
                throw new JsonException("A symbol filter cannot be parsed because the filter type property is missing.");
            }

            switch (type)
            {
                case "PRICE_FILTER":
                    return ParseAbsolutePriceFilter(objectProps);
                case "PERCENT_PRICE":
                    return ParseRelativePriceFilter(objectProps);
                case "PERCENT_PRICE_BY_SIDE":
                    return ParseRelativePriceBySideFilter(objectProps);
                case "LOT_SIZE":
                    return ParseLotSizeFilter(objectProps);
                case "MIN_NOTIONAL":
                    return ParseMinNotionalFilter(objectProps);
                case "NOTIONAL":
                    return ParseNotionalRangeFilter(objectProps);
                case "ICEBERG_PARTS":
                    return ParseIcebergPartsFilter(objectProps);
                case "MARKET_LOT_SIZE":
                    return ParseMarketLotSizeFilter(objectProps);
                case "MAX_NUM_ORDERS":
                    return ParseOpenOrdersFilter(objectProps);
                case "MAX_NUM_ALGO_ORDERS":
                    return ParseAlgoOrdesrFilter(objectProps);
                case "MAX_NUM_ICEBERG_ORDERS":
                    return ParseIcebergOrdersFilter(objectProps);
                case "MAX_POSITION":
                    return ParseMaxPositionFilter(objectProps);
                case "TRAILING_DELTA":
                    return ParseTrailingDeltaFilter(objectProps);
                default:
                    throw new JsonException($"An unknown type of the symbol filter \"{type}\" was encountered.");
            }
        }

        private static AbsolutePriceFilter ParseAbsolutePriceFilter(List<KeyValuePair<string, object>> objectProps)
        {
            AbsolutePriceFilter result = new AbsolutePriceFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "minPrice":
                        ParseDecimal(pair.Key, (string)pair.Value, out decimal minPrice);
                        if (minPrice > 0.0m) result.MinPrice = minPrice;
                        break;
                    case "maxPrice":
                        ParseDecimal(pair.Key, (string)pair.Value, out decimal maxPrice);
                        if (maxPrice > 0.0m) result.MaxPrice = maxPrice;
                        break;
                    case "tickSize":
                        ParseDecimal(pair.Key, (string)pair.Value, out decimal tickSize);
                        if (tickSize > 0.0m) result.TickSize = tickSize;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static RelativePriceFilter ParseRelativePriceFilter(List<KeyValuePair<string, object>> objectProps)
        {
            RelativePriceFilter result = new RelativePriceFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "multiplierUp":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.MultiplierUp);
                        break;
                    case "multiplierDown":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.MultiplierDown);
                        break;
                    case "multiplierDecimal":
                        // The designation of the property is unknown, and it is not stored.
                        break;
                    case "avgPriceMins":
                        uint mins = (uint)(long)pair.Value;;
                        if (mins != 0) result.AvgPriceInterval = mins;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static RelativePriceBySideFilter ParseRelativePriceBySideFilter(List<KeyValuePair<string, object>> objectProps)
        {
            RelativePriceBySideFilter result = new RelativePriceBySideFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "bidMultiplierUp":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.BidMultiplierUp);
                        break;
                    case "bidMultiplierDown":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.BidMultiplierDown);
                        break;
                    case "askMultiplierUp":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.AskMultiplierUp);
                        break;
                    case "askMultiplierDown":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.AskMultiplierDown);
                        break;
                    case "avgPriceMins":
                        uint mins = (uint)(long)pair.Value;;
                        if (mins != 0) result.AvgPriceInterval = mins;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static LotSizeFilter ParseLotSizeFilter(List<KeyValuePair<string, object>> objectProps)
        {
            LotSizeFilter result = new LotSizeFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "minQty":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.MinQuantity);
                        break;
                    case "maxQty":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.MaxQuantity);
                        break;
                    case "stepSize":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.StepSize);
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static MinNotionalFilter ParseMinNotionalFilter(List<KeyValuePair<string, object>> objectProps)
        {
            MinNotionalFilter result = new MinNotionalFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "notional":
                    case "minNotional":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.MinNotional);
                        break;
                    case "applyToMarket":
                        result.DoesApplyToMarket = (bool)pair.Value;
                        break;
                    case "avgPriceMins":
                        uint mins = (uint)(long)pair.Value;;
                        if (mins != 0) result.AvgPriceInterval = mins;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static NotionalRangeFilter ParseNotionalRangeFilter(List<KeyValuePair<string, object>> objectProps)
        {
            NotionalRangeFilter result = new NotionalRangeFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "minNotional":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.MinNotional);
                        break;
                    case "applyMinToMarket":
                        result.IsMinAppliedToMarket = (bool)pair.Value;
                        break;
                    case "maxNotional":
                        ParseDecimal(pair.Key, (string)pair.Value, out result.MaxNotional);
                        break;
                    case "applyMaxToMarket":
                        result.IsMaxAppliedToMarket = (bool)pair.Value;
                        break;
                    case "avgPriceMins":
                        uint mins = (uint)(long)pair.Value;;
                        if (mins != 0) result.AvgPriceInterval = mins;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static IcebergPartsFilter ParseIcebergPartsFilter(List<KeyValuePair<string, object>> objectProps)
        {
            IcebergPartsFilter result = new IcebergPartsFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "limit":
                        result.Limit = (uint)(long)pair.Value;;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static MarketLotSizeFilter ParseMarketLotSizeFilter(List<KeyValuePair<string, object>> objectProps)
        {
            MarketLotSizeFilter result = new MarketLotSizeFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "minQty":
                        ParseDouble(pair.Key, (string)pair.Value, out double minQty);
                        result.MinQuantity = minQty;
                        break;
                    case "maxQty":
                        ParseDouble(pair.Key, (string)pair.Value, out double maxQty);
                        result.MaxQuantity = maxQty;
                        break;
                    case "stepSize":
                        ParseDouble(pair.Key, (string)pair.Value, out double stepSize);
                        result.StepSize = stepSize;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static OpenOrdersFilter ParseOpenOrdersFilter(List<KeyValuePair<string, object>> objectProps)
        {
            OpenOrdersFilter result = new OpenOrdersFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "limit":
                    case "maxNumOrders":
                        result.Limit = (uint)(long)pair.Value;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static AlgoOrdersFilter ParseAlgoOrdesrFilter(List<KeyValuePair<string, object>> objectProps)
        {
            AlgoOrdersFilter result = new AlgoOrdersFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "limit":
                    case "maxNumAlgoOrders":
                        result.Limit = (uint)(long)pair.Value;;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static IcebergOrdersFilter ParseIcebergOrdersFilter(List<KeyValuePair<string, object>> objectProps)
        {
            IcebergOrdersFilter result = new IcebergOrdersFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "maxNumIcebergOrders":
                        result.Limit = (uint)(long)pair.Value;;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static MaxPositionFilter ParseMaxPositionFilter(List<KeyValuePair<string, object>> objectProps)
        {
            MaxPositionFilter result = new MaxPositionFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "maxPosition":
                        ParseDouble(pair.Key, (string)pair.Value, out result.MaxPosition);
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        private static TrailingDeltaFilter ParseTrailingDeltaFilter(List<KeyValuePair<string, object>> objectProps)
        {
            TrailingDeltaFilter result = new TrailingDeltaFilter();

            foreach (KeyValuePair<string, object> pair in objectProps)
            {
                switch (pair.Key)
                {
                    case "minTrailingAboveDelta":
                        result.MinTrailingAboveDelta = (uint)(long)pair.Value;;
                        break;
                    case "maxTrailingAboveDelta":
                        result.MaxTrailingAboveDelta = (uint)(long)pair.Value;;
                        break;
                    case "minTrailingBelowDelta":
                        result.MinTrailingBelowDelta = (uint)(long)pair.Value;;
                        break;
                    case "maxTrailingBelowDelta":
                        result.MaxTrailingBelowDelta = (uint)(long)pair.Value;;
                        break;
                    default:
                        throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
                }
            }

            return result;
        }

        // Utility methods.
        public static void ParseDouble(string propName, string s, out double value)
        {
            if (!double.TryParse(s, NumberStyles.Float, CommonUtility.NumberFormat, out value))
                throw new JsonException($"A value of the property \"{propName}\" cannot be parsed to a float number: \"{s}\".");
        }

        public static void ParseDecimal(string propName, string s, out decimal value)
        {
            if (!decimal.TryParse(s, NumberStyles.Float, CommonUtility.NumberFormat, out value))
                throw new JsonException($"A value of the property \"{propName}\" cannot be parsed to a float number: \"{s}\".");
        }

        #endregion
    }
}
