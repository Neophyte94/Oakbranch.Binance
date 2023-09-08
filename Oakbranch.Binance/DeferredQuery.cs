using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Oakbranch.Binance
{
    internal sealed class DeferredQuery<T> : IDeferredQuery<T>
    {
        #region Instance members

        private QueryParams m_Params;
        internal QueryParams Params => m_Params;

        private ReadOnlyCollection<QueryWeight> m_Weights;
        public IReadOnlyList<QueryWeight> Weights
        {
            get
            {
                ThrowIfDisposed();
                return m_Weights;
            }
        }

        private ExecuteQueryHandler<T> m_ExecuteHandler;
        private IReadOnlyDictionary<string, int> m_HeadersToLimitsMap;
        private ParseResponseHandler<T> m_ParseHandler;
        private object m_ParseArgs;
        private bool m_IsDisposed;

        #endregion

        #region Instance constructors

        internal DeferredQuery(
            QueryParams query,
            ExecuteQueryHandler<T> executeHandler,
            ParseResponseHandler<T> parseHandler,
            object parseArgs = null,
            IList<QueryWeight> weights = null,
            IReadOnlyDictionary<string, int> headersToLimitsMap = null)
        {
            if (query.IsUndefined)
                throw new ArgumentNullException(nameof(query), $"The specified query parameters are empty.");
            m_ExecuteHandler = executeHandler ?? throw new ArgumentNullException(nameof(executeHandler));
            m_ParseHandler = parseHandler ?? throw new ArgumentNullException(nameof(parseHandler));
            m_Params = query;
            m_Weights = new ReadOnlyCollection<QueryWeight>(weights ?? new QueryWeight[0]);
            m_ParseArgs = parseArgs;
            m_HeadersToLimitsMap = headersToLimitsMap;
        }

        #endregion

        #region Instance methods

        public Task<T> ExecuteAsync(CancellationToken ct)
        {
            ThrowIfDisposed();
            return m_ExecuteHandler(m_Params, m_Weights, m_ParseHandler, m_ParseArgs, m_HeadersToLimitsMap, ct);
        }

        private void ThrowIfDisposed()
        {
            if (m_IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public override string ToString()
        {
            string text = $"Deferred query ({typeof(T).Name}): ";
            if (m_IsDisposed) return text + "(Disposed)";

            text += $"{m_Params.Method} {m_Params.BaseEndpoint}{m_Params.RelativeEndpoint}";
            if (m_Params.QueryString != null)
                text += "?" + m_Params.QueryString.ToQuery();

            return text;
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;
            m_IsDisposed = true;

            m_ExecuteHandler = null;
            m_ParseHandler = null;
            m_ParseArgs = null;
            m_HeadersToLimitsMap = null;
            m_Params = default;
            m_Weights = null;
        }

        #endregion
    }
}
