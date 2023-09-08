﻿using System;
using System.Collections.Generic;

namespace Oakbranch.Binance
{
    /// <summary>
    /// Encapsulates information on a response to a web query.
    /// </summary>
    public class Response
    {
        #region Instance members

        public readonly byte[] Content;
        public readonly List<KeyValuePair<string, string>> LimitsUsage;
        public readonly bool IsSuccessful;

        #endregion

        #region Instance constructors

        /// <summary>
        /// Creates an instance representing a successful response.
        /// </summary>
        public Response(byte[] content, List<KeyValuePair<string, string>> limitsUsage)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            LimitsUsage = limitsUsage ?? throw new ArgumentNullException(nameof(limitsUsage));
            IsSuccessful = true;
        }

        /// <summary>
        /// Creates an instance representing a failed response.
        /// </summary>
        /// <param name="errorContent"></param>
        public Response(byte[] errorContent)
        {
            Content = errorContent;
            LimitsUsage = new List<KeyValuePair<string, string>>(0);
            IsSuccessful = false;
        }

        #endregion
    }
}
