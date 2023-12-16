using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Oakbranch.Binance.Utility;

public static class ExceptionUtility
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if the string value is <see langword="null"/>,
    /// or <see cref="ArgumentException"/> if it is empty or whitespace.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public static void ThrowIfNullOrWhitespace(
        [NotNull] this string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw value == null
                ? throw new ArgumentNullException(paramName)
                : throw new ArgumentException("The value cannot be empty or whitespace.", paramName);
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the string value is empty or whitespace.
    /// <para>Not triggers on <see langword="null"/> value.</para>
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public static void ThrowIfEmptyOrWhitespace(
        this string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value != null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The value cannot be empty or whitespace.", paramName);
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if both time constraints are not null,
    /// and <paramref name="to"/> is less than <paramref name="from"/>.
    /// </summary>
    /// <exception cref="ArgumentException/>
    public static void ThrowIfInvalidPeriod(DateTime? from, DateTime? to)
    {
        if (from != null && to != null && to.Value < from.Value)
        {
            throw new ArgumentException($"The specified time period [{from} ; {to}] is invalid.");
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the limit is negative, zero, or greater than the specified maximum.
    /// <para>Not triggers on <see langword="null"/> value.</para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void ThrowIfInvalidLimit(
        this int? value,
        int maxValue,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 1 || value > maxValue)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                $"The specified limit ({value}) is outside of the acceptable range [0 ; {maxValue}].");
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is less or equal to 0.0.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void ThrowIfLessOrEqualToZero(
        this decimal value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0.0m)
        {
            throw new ArgumentOutOfRangeException($"The specified value ({value}) is negative or zero", paramName);
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the value 
    /// is not <see langword="null"/> and it is less or equal to 0.0.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public static void ThrowIfLessOrEqualToZero(
        this decimal? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value != null)
        {
            ThrowIfLessOrEqualToZero(value.Value, paramName);
        }
    }
}