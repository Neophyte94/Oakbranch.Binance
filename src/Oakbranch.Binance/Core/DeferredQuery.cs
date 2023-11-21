using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Oakbranch.Binance.Abstractions;

namespace Oakbranch.Binance.Core;

internal sealed class DeferredQuery<T> : IDeferredQuery<T>
{
    #region Instance props & fields

    private QueryParams _params;
    internal QueryParams Params => _params;

    private ReadOnlyCollection<QueryWeight> _weights;
    public IReadOnlyList<QueryWeight> Weights
    {
        get
        {
            ThrowIfDisposed();
            return _weights;
        }
    }

    private ExecuteQueryHandler<T> _executeHandler;
    private IReadOnlyDictionary<string, int>? _headersToLimitsMap;
    private ParseResponseHandler<T> _parseHandler;
    private object? _parseArgs;
    private bool _isDisposed;

    #endregion

    #region Instance constructors

    internal DeferredQuery(
        QueryParams query,
        ExecuteQueryHandler<T> executeHandler,
        ParseResponseHandler<T> parseHandler,
        object? parseArgs = null,
        IList<QueryWeight>? weights = null,
        IReadOnlyDictionary<string, int>? headersToLimitsMap = null)
    {
        if (query.IsUndefined)
            throw new ArgumentNullException(nameof(query), $"The specified query parameters are empty.");
        _executeHandler = executeHandler ?? throw new ArgumentNullException(nameof(executeHandler));
        _parseHandler = parseHandler ?? throw new ArgumentNullException(nameof(parseHandler));
        _params = query;
        _weights = new ReadOnlyCollection<QueryWeight>(weights ?? Array.Empty<QueryWeight>());
        _parseArgs = parseArgs;
        _headersToLimitsMap = headersToLimitsMap;
    }

    #endregion

    #region Instance methods

    public Task<T> ExecuteAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        return _executeHandler(_params, _weights, _parseHandler, _parseArgs, _headersToLimitsMap, ct);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    private void Dispose(bool releaseManaged)
    {
        _isDisposed = true;
        if (releaseManaged)
        {
            _executeHandler = null!;
            _parseHandler = null!;
            _parseArgs = null;
            _headersToLimitsMap = null;
            _params = default;
            _weights = null!;
        }
    }

    public override string ToString()
    {
        string text = $"Deferred query ({typeof(T).Name}): ";
        if (_isDisposed) return text + "(Disposed)";

        text += $"{_params.Method} {_params.BaseEndpoint}{_params.RelativeEndpoint}";
        if (_params.QueryString != null)
            text += "?" + _params.QueryString.ToQuery();

        return text;
    }

    #endregion

    #region Destructor

    ~DeferredQuery()
    {
        if (!_isDisposed)
        {
            Dispose(false);
        }
    }

    #endregion
}
