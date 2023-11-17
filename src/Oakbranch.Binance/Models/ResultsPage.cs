using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Models
{
    public class ResultsPage<T> : List<T>
    {
        private int _total;
        /// <summary>
        /// Gets or sets the total number of results in all pages.
        /// </summary>
        public int Total
        {
            get
            {
                return _total;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Total));
                _total = value;
            }
        }

        public ResultsPage(int capacity) : base(capacity) { }
    }
}