using System;
using System.Text;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Encapsulates functions for constructing query strings for HTTP requests.
    /// </summary>
    public sealed class QueryBuilder
    {
        private readonly StringBuilder m_Container;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBuilder"/> class.
        /// </summary>
        /// <param name="capacity">The initial capacity of the query string. Default is 100.</param>
        public QueryBuilder(int capacity = 100)
        {
            m_Container = new StringBuilder(capacity);
        }

        /// <summary>
        /// Adds a parameter with the specified name and string value to the query string.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddParameter(string name, string value)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            if (m_Container.Length != 0)
                m_Container.Append('&');
            m_Container.Append(name);
            m_Container.Append('=');
            m_Container.Append(value);
        }

        /// <summary>
        /// Adds a parameter with the specified name and array of string values to the query string.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="values">The array of values of the parameter.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddParameter(string name, string[] values)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (m_Container.Length != 0)
                m_Container.Append('&');

            m_Container.Append(name);
            m_Container.Append("=[");
            for (int i = 0; i != values.Length; ++i)
            {
                if (i != 0) m_Container.Append(',');
                m_Container.Append('"');
                m_Container.Append(values[i]);
                m_Container.Append('"');
            }
            m_Container.Append(']');
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
        public string ToQuery() => m_Container.ToString();
    }
}