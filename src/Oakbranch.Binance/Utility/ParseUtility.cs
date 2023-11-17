using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Oakbranch.Binance.Models;
using Oakbranch.Binance.Models.Filters.Exchange;
using Oakbranch.Binance.Models.Filters.Symbol;

namespace Oakbranch.Binance.Utility;

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
        {
            throw new JsonException($"An object start was expected but encountered \"{reader.TokenType}\".");
        }
    }

    public static void EnsureObjectStartToken(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"An object start was expected but encountered \"{reader.TokenType}\".");
        }
    }

    public static void ReadObjectEnd(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException($"An object end was expected but encountered \"{reader.TokenType}\".");
        }
    }

    public static void ReadArrayStart(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"An array's start was expected but encountered \"{reader.TokenType}\".");
        }
    }

    public static void EnsureArrayStartToken(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"An array's start was expected but encountered \"{reader.TokenType}\".");
        }
    }

    public static void ReadArrayEnd(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException($"An array's end was expected but encountered \"{reader.TokenType}\".");
        }
    }

    /// <summary>
    /// Reads the next JSON token as a property name from the given JSON reader,
    /// ensuring that it is neither <see langword="null"/> nor empty.
    /// </summary>
    /// <param name="reader">The JSON reader to read from.</param>
    /// <returns>The property name read.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the read token is not a property name, or its value is either <see langword="null"/> or empty.
    /// </exception>
    public static string ReadNonEmptyPropertyName(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException($"A property's name was expected but \"{reader.TokenType}\" encountered.");
        }

        return GetNonEmptyPropertyName(ref reader);
    }

    /// <summary>
    /// Reads the current JSON token from the given JSON reader as a property name,
    /// validating its type, and ensuring that it is neither <see langword="null"/> nor empty.
    /// </summary>
    /// <returns>The property name read.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the current token is not a property name, or its value is either <see langword="null"/> or empty.
    /// </exception>
    public static string GetNonEmptyPropertyName(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException($"A property's name was expected but \"{reader.TokenType}\" encountered.");
        }

        string? propName = reader.GetString();
        if (string.IsNullOrEmpty(propName))
        {
            throw new JsonException("Null or empty property name was encountered.");
        }

        return propName;
    }

    /// <summary>
    /// Reads the next JSON token as a property name from the given JSON reader, ensuring that it matches the specified name.
    /// </summary>
    /// <param name="reader">The JSON reader to read from.</param>
    /// <param name="propName">The expected name of the property.</param>
    /// <exception cref="JsonException">Thrown when the reader encounters a token that is not a property name, or when the property name
    /// read from the reader does not match the expected property name.</exception>
    public static void ReadExactPropertyName(ref Utf8JsonReader reader, string propName)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException($"A propertie's name was expected but \"{reader.TokenType}\" encountered.");
        }
        if (reader.GetString() != propName)
        {
            throw new JsonException($"The property \"{propName}\" was expected but \"{reader.GetString()}\" encountered.");
        }
    }

    public static void EnsurePropertyNameToken(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException($"A propertie's name was expected but \"{reader.TokenType}\" encountered.");
        }
    }

    /// <summary>
    /// Validates the current JSON token to be of the given type.
    /// </summary>
    /// <exception cref="JsonException"/>
    public static void EnsurePropertyValueToken(
        ref Utf8JsonReader reader,
        JsonTokenType requiredToken,
        string propName)
    {
        if (reader.TokenType != requiredToken)
        {
            throw GenerateInvalidValueTypeException(propName, requiredToken, reader.TokenType);
        }
    }

    /// <summary>
    /// Reads the current JSON token from the given JSON reader as a string value,
    /// validating its type, and ensuring that it is neither <see langword="null"/> nor empty.
    /// </summary>
    /// <param name="reader">The JSON reader to get the current value from.</param>
    /// <returns>The property name read.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the current token is not a string, or its value is either <see langword="null"/> or empty.
    /// </exception>
    public static string ReadNonEmptyString(ref Utf8JsonReader reader, string propName)
    {
        if (!reader.Read())
        {
            throw GenerateNoPropertyValueException(propName);
        }

        return GetNonEmptyString(ref reader, propName);
    }

    /// <summary>
    /// Reads the current JSON token from the given JSON reader as a string value,
    /// validating its type, and ensuring that it is neither <see langword="null"/> nor empty.
    /// </summary>
    /// <param name="reader">The JSON reader to get the current value from.</param>
    /// <returns>The property name read.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the current token is not a string, or its value is either <see langword="null"/> or empty.
    /// </exception>
    public static string GetNonEmptyString(ref Utf8JsonReader reader, string propName)
    {
        EnsurePropertyValueToken(ref reader, JsonTokenType.String, propName);

        string? value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException($"The read value of the property \"{propName}\" is either null or empty.");
        }

        return value;
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
    public static JsonException GenerateMissingPropertyException(string objName, string propName)
    {
        return new JsonException($"The {propName} property of the {objName} object is missing in the response.");
    }

    public static JsonException GenerateNoPropertyValueException(string propName)
    {
        return new JsonException(
            $"A value of the property \"{propName}\" was expected," +
            $"but the end of the data was reached.");
    }

    public static JsonException GenerateInvalidValueTypeException(
        string propName,
        JsonTokenType expectedType,
        JsonTokenType actualType)
    {
        return new JsonException(
            $"The {propName} value of the type \"{expectedType}\" was expected but \"{actualType}\" encountered.");
    }

    public static JsonException GenerateUnknownPropertyException(string propName)
    {
        return new JsonException($"An unknown property was encountered: \"{propName}\".");
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

        string? message = reader.GetString();

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
        if (string.IsNullOrWhiteSpace(s))
            throw new JsonException("The order status value is null.");

        return s switch
        {
            "NEW" => OrderStatus.New,
            "PARTIALLY_FILLED" => OrderStatus.PartiallyFilled,
            "FILLED" => OrderStatus.Filled,
            "CANCELED" => OrderStatus.Canceled,
            "PENDING_CANCEL" => throw new NotImplementedException($"The order status \"s\" is not implemented for it was considered unused."),
            "REJECTED" => OrderStatus.Rejected,
            "EXPIRED" => OrderStatus.Expired,
            "EXPIRED_IN_MATCH" => OrderStatus.ExpiredInMatch,
            _ => throw new JsonException($"An unknown order status \"{s}\" was encountered."),
        };
    }

    public static OrderSide ParseOrderSide(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new JsonException($"The order side value is null.");

        return s switch
        {
            "BUY" => OrderSide.Buy,
            "SELL" => OrderSide.Sell,
            _ => throw new JsonException($"An unknown order side \"{s}\" was encountered."),
        };
    }

    public static List<OrderPartialFill> ParseOrderPartialFills(ref Utf8JsonReader reader)
    {
        EnsureArrayStartToken(ref reader);
        List<OrderPartialFill> resultsList = new List<OrderPartialFill>(8);

        ParseSchemaValidator validator = new ParseSchemaValidator(5);
        decimal price = 0.0m, qty = 0.0m, comm = 0.0m;
        long id = 0;
        string? commAsset = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            EnsureObjectStartToken(ref reader);

            // Parse partial fill properties.
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string propName = GetNonEmptyPropertyName(ref reader);

                if (!reader.Read())
                {
                    throw GenerateNoPropertyValueException(propName);
                }

                switch (propName)
                {
                    case "price":
                        ParseDecimal(propName, reader.GetString(), out price);
                        validator.RegisterProperty(0);
                        break;
                    case "qty":
                        ParseDecimal(propName, reader.GetString(), out qty);
                        validator.RegisterProperty(1);
                        break;
                    case "commission":
                        ParseDecimal(propName, reader.GetString(), out comm);
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
                        throw GenerateUnknownPropertyException(propName);
                }
            }

            // Check whether all the essential properties were provided.
            if (!validator.IsComplete())
            {
                const string objName = "order partial fill";
                int missingPropNum = validator.GetMissingPropertyNumber();
                throw missingPropNum switch
                {
                    0 => GenerateMissingPropertyException(objName, "price"),
                    1 => GenerateMissingPropertyException(objName, "quantity"),
                    2 => GenerateMissingPropertyException(objName, "commission"),
                    3 => GenerateMissingPropertyException(objName, "commission asset"),
                    4 => GenerateMissingPropertyException(objName, "trade ID"),
                    _ => GenerateMissingPropertyException(objName, $"unknown ({missingPropNum})"),
                };
            }

            // Add the partial fill to the results list.
            resultsList.Add(new OrderPartialFill(id, price, qty, comm, commAsset!));
            validator.Reset();
        }

        return resultsList;
    }

    // Exchange filters.
    public static ExchangeFilter ParseExchangeFilter(ref Utf8JsonReader reader)
    {
        EnsureObjectStartToken(ref reader);

        const string propName = "filterType";
        ReadExactPropertyName(ref reader, propName);
        string? type = ReadNonEmptyString(ref reader, propName);

        return type switch
        {
            "EXCHANGE_MAX_NUM_ORDERS" => ParseTotalOpenOrdersFilter(ref reader),
            "EXCHANGE_MAX_ALGO_ORDERS" => ParseTotalAlgoOrdersFilter(ref reader),
            _ => throw new JsonException($"An unknown exchange filter type was encountered: \"{type}\"."),
        };
    }

    private static TotalOpenOrdersFilter ParseTotalOpenOrdersFilter(ref Utf8JsonReader reader)
    {
        TotalOpenOrdersFilter result = new TotalOpenOrdersFilter();

        const string propName = "maxNumOrders";
        ReadExactPropertyName(ref reader, propName);

        if (!reader.Read())
        {
            throw GenerateNoPropertyValueException(propName);
        }
        EnsurePropertyValueToken(ref reader, JsonTokenType.Number, propName);
        result.Limit = reader.GetUInt32();

        ReadObjectEnd(ref reader);

        return result;
    }

    private static TotalAlgoOrdersFilter ParseTotalAlgoOrdersFilter(ref Utf8JsonReader reader)
    {
        TotalAlgoOrdersFilter result = new TotalAlgoOrdersFilter();

        const string propName = "maxNumAlgoOrders";
        ReadExactPropertyName(ref reader, propName);

        if (!reader.Read())
        {
            throw GenerateNoPropertyValueException(propName);
        }
        EnsurePropertyValueToken(ref reader, JsonTokenType.Number, propName);
        result.Limit = reader.GetUInt32();

        ReadObjectEnd(ref reader);

        return result;
    }

    // Symbol filters.
    public static SymbolFilter ParseSymbolFilter(ref Utf8JsonReader reader)
    {
        EnsureObjectStartToken(ref reader);

        List<KeyValuePair<string, object?>> objectProps = new List<KeyValuePair<string, object?>>(5);
        string? type = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string propName = GetNonEmptyPropertyName(ref reader);

            if (!reader.Read())
                throw GenerateNoPropertyValueException(propName);

            if (propName == "filterType")
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException(
                        $"A symbol filter cannot be parsed: " +
                        $"the type property has an invalid value token ({reader.TokenType}).");
                }

                type = reader.GetString();
                if (string.IsNullOrWhiteSpace(type))
                {
                    throw new JsonException(
                        $"A symbol filter cannot be parsed: the value of the type property is null.");
                }

                continue;
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    objectProps.Add(new KeyValuePair<string, object?>(propName, reader.GetString()));
                    break;
                case JsonTokenType.Number:
                    objectProps.Add(new KeyValuePair<string, object?>(propName, reader.GetInt64()));
                    break;
                case JsonTokenType.True:
                    objectProps.Add(new KeyValuePair<string, object?>(propName, true));
                    break;
                case JsonTokenType.False:
                    objectProps.Add(new KeyValuePair<string, object?>(propName, false));
                    break;
                case JsonTokenType.Null:
                    objectProps.Add(new KeyValuePair<string, object?>(propName, null));
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

        return type switch
        {
            "PRICE_FILTER" => ParseAbsolutePriceFilter(objectProps),
            "PERCENT_PRICE" => ParseRelativePriceFilter(objectProps),
            "PERCENT_PRICE_BY_SIDE" => ParseRelativePriceBySideFilter(objectProps),
            "LOT_SIZE" => ParseLotSizeFilter(objectProps),
            "MIN_NOTIONAL" => ParseMinNotionalFilter(objectProps),
            "NOTIONAL" => ParseNotionalRangeFilter(objectProps),
            "ICEBERG_PARTS" => ParseIcebergPartsFilter(objectProps),
            "MARKET_LOT_SIZE" => ParseMarketLotSizeFilter(objectProps),
            "MAX_NUM_ORDERS" => ParseOpenOrdersFilter(objectProps),
            "MAX_NUM_ALGO_ORDERS" => ParseAlgoOrdesrFilter(objectProps),
            "MAX_NUM_ICEBERG_ORDERS" => ParseIcebergOrdersFilter(objectProps),
            "MAX_POSITION" => ParseMaxPositionFilter(objectProps),
            "TRAILING_DELTA" => ParseTrailingDeltaFilter(objectProps),
            _ => throw new JsonException($"An unknown type of the symbol filter \"{type}\" was encountered."),
        };
    }

    private static AbsolutePriceFilter ParseAbsolutePriceFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        AbsolutePriceFilter result = new AbsolutePriceFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "minPrice":
                    ParseDecimal(pair.Key, pair.Value as string, out decimal minPrice);
                    if (minPrice > 0.0m) result.MinPrice = minPrice;
                    break;
                case "maxPrice":
                    ParseDecimal(pair.Key, pair.Value as string, out decimal maxPrice);
                    if (maxPrice > 0.0m) result.MaxPrice = maxPrice;
                    break;
                case "tickSize":
                    ParseDecimal(pair.Key, pair.Value as string, out decimal tickSize);
                    if (tickSize > 0.0m) result.TickSize = tickSize;
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    private static RelativePriceFilter ParseRelativePriceFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        RelativePriceFilter result = new RelativePriceFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "multiplierUp":
                    ParseDecimal(pair.Key, pair.Value as string, out result.MultiplierUp);
                    break;
                case "multiplierDown":
                    ParseDecimal(pair.Key, pair.Value as string, out result.MultiplierDown);
                    break;
                case "multiplierDecimal":
                    // The designation of the property is unknown, and it is not stored.
                    break;
                case "avgPriceMins":
                    uint mins = (uint)(long)pair.Value!;
                    if (mins != 0) result.AvgPriceInterval = mins;
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    private static RelativePriceBySideFilter ParseRelativePriceBySideFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        RelativePriceBySideFilter result = new RelativePriceBySideFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "bidMultiplierUp":
                    ParseDecimal(pair.Key, pair.Value as string, out result.BidMultiplierUp);
                    break;
                case "bidMultiplierDown":
                    ParseDecimal(pair.Key, pair.Value as string, out result.BidMultiplierDown);
                    break;
                case "askMultiplierUp":
                    ParseDecimal(pair.Key, pair.Value as string, out result.AskMultiplierUp);
                    break;
                case "askMultiplierDown":
                    ParseDecimal(pair.Key, pair.Value as string, out result.AskMultiplierDown);
                    break;
                case "avgPriceMins":
                    uint mins = (uint)(long)pair.Value!;
                    if (mins != 0) result.AvgPriceInterval = mins;
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    private static LotSizeFilter ParseLotSizeFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        LotSizeFilter result = new LotSizeFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "minQty":
                    ParseDecimal(pair.Key, pair.Value as string, out result.MinQuantity);
                    break;
                case "maxQty":
                    ParseDecimal(pair.Key, pair.Value as string, out result.MaxQuantity);
                    break;
                case "stepSize":
                    ParseDecimal(pair.Key, pair.Value as string, out result.StepSize);
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    private static MinNotionalFilter ParseMinNotionalFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        MinNotionalFilter result = new MinNotionalFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "notional":
                case "minNotional":
                    ParseDecimal(pair.Key, pair.Value as string, out result.MinNotional);
                    break;
                case "applyToMarket":
                    result.DoesApplyToMarket = (bool)pair.Value!;
                    break;
                case "avgPriceMins":
                    uint mins = (uint)(long)pair.Value!;
                    if (mins != 0) result.AvgPriceInterval = mins;
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    private static NotionalRangeFilter ParseNotionalRangeFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        NotionalRangeFilter result = new NotionalRangeFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "minNotional":
                    ParseDecimal(pair.Key, pair.Value as string, out result.MinNotional);
                    break;
                case "applyMinToMarket":
                    result.IsMinAppliedToMarket = (bool)pair.Value!;
                    break;
                case "maxNotional":
                    ParseDecimal(pair.Key, pair.Value as string, out result.MaxNotional);
                    break;
                case "applyMaxToMarket":
                    result.IsMaxAppliedToMarket = (bool)pair.Value!;
                    break;
                case "avgPriceMins":
                    uint mins = (uint)(long)pair.Value!;
                    if (mins != 0) result.AvgPriceInterval = mins;
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    private static IcebergPartsFilter ParseIcebergPartsFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        IcebergPartsFilter result = new IcebergPartsFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            result.Limit = pair.Key switch
            {
                "limit" => (uint)(long)pair.Value!,
                _ => throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered."),
            };
        }

        return result;
    }

    private static MarketLotSizeFilter ParseMarketLotSizeFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        MarketLotSizeFilter result = new MarketLotSizeFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "minQty":
                    ParseDouble(pair.Key, pair.Value as string, out double minQty);
                    result.MinQuantity = minQty;
                    break;
                case "maxQty":
                    ParseDouble(pair.Key, pair.Value as string, out double maxQty);
                    result.MaxQuantity = maxQty;
                    break;
                case "stepSize":
                    ParseDouble(pair.Key, pair.Value as string, out double stepSize);
                    result.StepSize = stepSize;
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    private static OpenOrdersFilter ParseOpenOrdersFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        OpenOrdersFilter result = new OpenOrdersFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            result.Limit = pair.Key switch
            {
                "limit" or "maxNumOrders" => (uint)(long)pair.Value!,
                _ => throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered."),
            };
        }

        return result;
    }

    private static AlgoOrdersFilter ParseAlgoOrdesrFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        AlgoOrdersFilter result = new AlgoOrdersFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            result.Limit = pair.Key switch
            {
                "limit" or "maxNumAlgoOrders" => (uint)(long)pair.Value!,
                _ => throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered."),
            };
        }

        return result;
    }

    private static IcebergOrdersFilter ParseIcebergOrdersFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        IcebergOrdersFilter result = new IcebergOrdersFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            result.Limit = pair.Key switch
            {
                "maxNumIcebergOrders" => (uint)(long)pair.Value!,
                _ => throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered."),
            };
        }

        return result;
    }

    private static MaxPositionFilter ParseMaxPositionFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        MaxPositionFilter result = new MaxPositionFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "maxPosition":
                    ParseDouble(pair.Key, pair.Value as string, out result.MaxPosition);
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    private static TrailingDeltaFilter ParseTrailingDeltaFilter(List<KeyValuePair<string, object?>> objectProps)
    {
        TrailingDeltaFilter result = new TrailingDeltaFilter();

        foreach (KeyValuePair<string, object?> pair in objectProps)
        {
            switch (pair.Key)
            {
                case "minTrailingAboveDelta":
                    result.MinTrailingAboveDelta = (uint)(long)pair.Value!;
                    break;
                case "maxTrailingAboveDelta":
                    result.MaxTrailingAboveDelta = (uint)(long)pair.Value!;
                    break;
                case "minTrailingBelowDelta":
                    result.MinTrailingBelowDelta = (uint)(long)pair.Value!;
                    break;
                case "maxTrailingBelowDelta":
                    result.MaxTrailingBelowDelta = (uint)(long)pair.Value!;
                    break;
                default:
                    throw new JsonException($"An unknown property \"{pair.Key}\" of the {result.Type} filter was encountered.");
            }
        }

        return result;
    }

    // Utility methods.
    public static void ParseDouble(string propName, string? s, out double value)
    {
        if (!double.TryParse(s, NumberStyles.Float, CommonUtility.NumberFormat, out value))
        {
            throw new JsonException(
                $"A value of the property \"{propName}\" cannot be parsed to a floating-point number: \"{s}\".");
        }
    }

    public static void ParseDecimal(string propName, string? s, out decimal value)
    {
        if (!decimal.TryParse(s, NumberStyles.Float, CommonUtility.NumberFormat, out value))
        {
            throw new JsonException(
                $"A value of the property \"{propName}\" cannot be parsed to a decimal number: \"{s}\".");
        }
    }

    #endregion
}
