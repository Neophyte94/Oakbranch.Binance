using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Oakbranch.Binance.UnitTests
{
    public abstract class ApiClientTestsBase
    {
        #region Constants

        protected const int GlobalSetUpTimeout = 10000; // in ms.
        protected const int GlobalSetUpRetryLimit = 3;
        protected const int DefaultTestTimeout = 10000; // in ms.
        protected const int DefaultTestRetryLimit = 2;
        private const string NullObjectDescription = "(null)";

        #endregion

        #region Static members

        protected static readonly DateTime ReferenceDateTime = new DateTime(2023, 11, 18);
        protected static readonly TimeSpan UtcTimeErrorTolerance = new TimeSpan(5 * TimeSpan.TicksPerMinute);

        public static object?[] NullAndWhitespaceStringCases { get; } = new object?[]
        {
            null,
            string.Empty,
            " "
        };

        #endregion

        #region Instance members

        protected readonly ILogger? Logger;

        private readonly bool _areQueryResultsLogged;
        protected bool AreQueryResultsLogged => _areQueryResultsLogged;

        #endregion

        #region Instance constructors

        public ApiClientTestsBase(ILogger? logger, bool areQueryResultsLogged)
        {
            Logger = logger;
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

        protected static ILogger<T> CreateDefaultLogger<T>(LogLevel level)
        {
            using ConsoleLoggerFactory factory = new ConsoleLoggerFactory(level);
            return factory.CreateLogger<T>();
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
            Logger?.Log(level, message);
        }

        /// <summary>
        /// Logs the type and public properties' values of the given object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="item">The object to log.</param>
        protected void LogObject<T>(T? item)
        {
            if (Logger == null)
            {
                return;
            }

            if (item == null)
            {
                LogMessage(LogLevel.Information, NullObjectDescription);
                return;
            }

            Type type = item.GetType();
            GetPublicMembers(type, out FieldInfo[]? fields, out PropertyInfo[]? props);
            string desc = GenerateDescription(type, fields, props, item);

            Logger?.Log(LogLevel.Information, desc);
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
            if (Logger == null)
            {
                return;
            }

            if (items == null)
            {
                LogMessage(LogLevel.Information, NullObjectDescription);
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

            LogMessage(LogLevel.Information, sb.ToString());
        }

        #endregion
    }
}
