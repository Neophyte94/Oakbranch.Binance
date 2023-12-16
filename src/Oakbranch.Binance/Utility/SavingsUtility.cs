using System;
using System.Text.Json;
using Oakbranch.Binance.Models.Savings;

namespace Oakbranch.Binance.Utility;

internal static class SavingsUtility
{
    public static string Format(SavingsProductType value)
    {
        return value switch
        {
            SavingsProductType.Flexible => "DAILY",
            SavingsProductType.Activity => "ACTIVITY",
            SavingsProductType.Fixed => "CUSTOMIZED_FIXED",
            _ => throw new NotImplementedException($"The savings product type \"{value}\" is not implemented."),
        };
    }

    public static SavingsProductType ParseLendingType(string s)
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
}