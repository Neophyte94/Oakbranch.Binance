using System;
using System.Collections.Generic;
using System.Text;
using Oakbranch.Binance.Utility;

namespace Oakbranch.Binance.Core;

/// <summary>
/// Encapsulates functions for constructing query strings for HTTP requests.
/// </summary>
public sealed class QueryBuilder
{
    private readonly StringBuilder _container;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBuilder"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity of the query string. Default is 100.</param>
    public QueryBuilder(int capacity = 100)
    {
        _container = new StringBuilder(capacity);
    }

    /// <summary>
    /// Adds a parameter with the specified name and string value to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public void AddParameter(string name, string value)
    {
        name.ThrowIfNullOrWhitespace();
        ArgumentNullException.ThrowIfNull(value);

        if (_container.Length != 0) { _container.Append('&'); }
        _container.Append(name);
        _container.Append('=');
        _container.Append(value);
    }

    /// <summary>
    /// Adds a parameter with the specified name and array of string values to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="values">The array of values of the parameter.</param>
    /// <exception cref="ArgumentNullException"/>
    public void AddParameter(string name, string[] values)
    {
        name.ThrowIfNullOrWhitespace();
        ArgumentNullException.ThrowIfNull(values);

        if (_container.Length != 0) { _container.Append('&'); }

        _container.Append(name);
        _container.Append("=[");

        for (int i = 0; i != values.Length; ++i)
        {
            if (i != 0) { _container.Append(','); }
            _container.Append('"');
            _container.Append(values[i]);
            _container.Append('"');
        }

        _container.Append(']');
    }

    /// <summary>
    /// Adds a parameter with the specified name and collection of string values to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="values">The collection of values of the parameter.</param>
    /// <exception cref="ArgumentNullException"/>
    public void AddParameter(string name, IEnumerable<string> values)
    {
        name.ThrowIfNullOrWhitespace();
        ArgumentNullException.ThrowIfNull(values);

        if (_container.Length != 0) { _container.Append('&'); }
        _container.Append(name);
        _container.Append("=[");

        bool isFirst = true;
        foreach (string val in values)
        {
            if (isFirst) { isFirst = false; }
            else { _container.Append(','); }

            _container.Append('"');
            _container.Append(val);
            _container.Append('"');
        }

        _container.Append(']');
    }

    /// <summary>
    /// Adds a parameter with the specified name and decimal value to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The decimal value of the parameter.</param>
    public void AddParameter(string name, decimal value) => AddParameter(name, value.ToString(CommonUtility.NumberFormat));

    /// <summary>
    /// Adds a parameter with the specified name and floating-point numeric value to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The floating-point numeric value of the parameter.</param>
    public void AddParameter(string name, double value) => AddParameter(name, value.ToString(CommonUtility.NumberFormat));

    /// <summary>
    /// Adds a parameter with the specified name and integer value to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The integer value of the parameter.</param>
    public void AddParameter(string name, int value) => AddParameter(name, value.ToString(CommonUtility.NumberFormat));

    /// <summary>
    /// Adds a parameter with the specified name and unsigned integer value to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The unsigned integer value of the parameter.</param>
    public void AddParameter(string name, uint value) => AddParameter(name, value.ToString(CommonUtility.NumberFormat));

    /// <summary>
    /// Adds a parameter with the specified name and integer value to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The integer value of the parameter.</param>
    public void AddParameter(string name, long value) => AddParameter(name, value.ToString(CommonUtility.NumberFormat));

    /// <summary>
    /// Adds a parameter with the specified name and boolean value to the query string.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The boolean value of the parameter.</param>
    public void AddParameter(string name, bool value) => AddParameter(name, value ? "TRUE" : "FALSE");

    /// <summary>
    /// Converts the provided query parameters to a query string.
    /// </summary>
    /// <returns>The query string containing the constructed parameters.</returns>
    public string ToQuery() => _container.ToString();
}