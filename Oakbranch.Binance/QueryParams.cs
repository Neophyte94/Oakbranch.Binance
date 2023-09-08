using System;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Contains execution parameters of a web query to Binance REST API.
    /// </summary>
    public readonly struct QueryParams
    {
        #region Instance members

        public readonly HttpMethod Method;
        public readonly string BaseEndpoint;
        public readonly string RelativeEndpoint;
        public readonly QueryBuilder QueryString;
        public readonly bool IsSecured;

        public bool IsUndefined => BaseEndpoint == null;

        #endregion

        #region Instance constructors

        public QueryParams(
            HttpMethod method, string baseEndpoint, string relativeEndpoint,
            QueryBuilder queryString = null, bool isSecured = false)
        {
            if (String.IsNullOrWhiteSpace(baseEndpoint))
                throw new ArgumentNullException(nameof(baseEndpoint));
            if (String.IsNullOrWhiteSpace(relativeEndpoint))
                throw new ArgumentNullException(nameof(relativeEndpoint));

            Method = method;
            BaseEndpoint = baseEndpoint;
            RelativeEndpoint = relativeEndpoint;
            QueryString = queryString;
            IsSecured = isSecured;
        }

        #endregion
    }
}
