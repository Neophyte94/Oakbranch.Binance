using System;
using System.Reflection;
using System.Text;
using Oakbranch.Common.Logging;

namespace Oakbranch.Binance.UnitTests
{
    public abstract class ApiClientTestsBase
    {
        #region Constants

        private const string NullObjectDescription = "(null)";

        #endregion

        #region Instance members

        private readonly ILogger? _logger;
        protected ILogger? Logger => _logger;

        private readonly bool _areQueryResultsLogged;
        protected bool AreQueryResultsLogged => _areQueryResultsLogged;

        protected abstract string LogContext { get; }

        #endregion

        #region Instance constructors

        public ApiClientTestsBase(ILogger? logger, bool areQueryResultsLogged)
        {
            _logger = logger;
            _areQueryResultsLogged = areQueryResultsLogged;
        }

        #endregion

        #region Static methods

        private static void GetPublicMembers(Type type, out FieldInfo[] fields, out PropertyInfo[] props)
        {
            static bool IsReadableProperty(PropertyInfo p)
            {
                if (!p.CanRead) return false;

                ParameterInfo[] indexParams = p.GetIndexParameters();
                return indexParams == null || indexParams.Length == 0;
            }

            fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            props = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(IsReadableProperty)
                .ToArray();
        }

        private static string GenerateDescription(Type type, FieldInfo[]? fields, PropertyInfo[]? props, object instance)
        {
            StringBuilder sb = new StringBuilder(
                type.Name.Length + 5 
                + 30 * (fields?.Length ?? 0) 
                + 30 * (props?.Length ?? 0));

            sb.Append($"{{{type.Name}}}: ");
            bool isFirst = true;

            if (fields != null)
            {
                foreach (FieldInfo f in fields)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append(f.Name);
                    sb.Append(" = ");
                    sb.Append(f.GetValue(instance));
                }
            }

            if (props != null)
            {
                foreach (PropertyInfo p in props)
                {
                    if (!p.CanRead)
                    {
                        continue;
                    }

                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    sb.Append(p.Name);
                    sb.Append(" = ");

                    object? val = p.GetValue(instance);
                    if (val == null)
                        sb.Append(NullObjectDescription);
                    else
                        sb.Append(val.ToString());
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Instance methods

        /// <summary>
        /// Pushes the specified message with the specified severity level to the logger.
        /// <para>If <see cref="Logger"/> is <see langword="null"/>, then does nothing.</para>
        /// </summary>
        /// <param name="level">The logging severity of the message pushed.</param>
        /// <param name="message">The message to push.</param>
        protected void LogMessage(LogLevel level, string message)
        {
            _logger?.Log(level, LogContext, message);
        }

        /// <summary>
        /// Logs the type and public properties' values of the given object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="item">The object to log.</param>
        protected void LogObject<T>(T? item)
        {
            if (_logger == null)
            {
                return;
            }

            if (item == null)
            {
                LogMessage(LogLevel.Info, NullObjectDescription);
                return;
            }

            Type type = item.GetType();
            GetPublicMembers(type, out FieldInfo[]? fields, out PropertyInfo[]? props);
            string desc = GenerateDescription(type, fields, props, item);

            _logger?.Log(LogLevel.Info, LogContext, desc);
        }

        /// <summary>
        /// Logs the type and public properties' values of each element in the given collection.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="items">The collection of elements to log.</param>
        /// <param name="limit">
        /// The maximum number of elements to log. If the limit is reached, the log stack is ended with the ellipsis symbols.
        /// <para>Use the value -1 to log all elements in the collection.</para>
        /// </param>
        protected void LogCollection<T>(IEnumerable<T> items, int limit = -1)
        {
            if (_logger == null)
            {
                return;
            }

            if (items == null)
            {
                LogMessage(LogLevel.Info, NullObjectDescription);
                return;
            }

            StringBuilder sb = new StringBuilder(limit > 0 ? limit * 100 : 1000);
            int counter = 0;
            bool isTrunc = false;
            if (limit == -1)
            {
                limit = int.MaxValue;
            }

            Type? type = null;
            FieldInfo[]? fields = null;
            PropertyInfo[]? props = null;

            foreach (T item in items)
            {
                if (++counter > limit)
                {
                    isTrunc = true;
                    continue;
                }

                if (item == null)
                {
                    sb.AppendLine(NullObjectDescription);
                    continue;
                }

                Type currType = item.GetType();
                if (type != currType)
                {
                    type = currType;
                    GetPublicMembers(type, out fields, out props);
                }

                string desc = GenerateDescription(type, fields, props, item);
                sb.AppendLine(desc);
            }

            if (isTrunc)
            {
                sb.AppendLine($"... (total: {counter})");
            }

            LogMessage(LogLevel.Info, sb.ToString());
        }

        #endregion
    }
}
