using System;
using System.Collections.Generic;

namespace Oakbranch.Binance
{
    public class ResultsPage<T> : List<T>
    {
        private int m_Total;
        /// <summary>
        /// Gets or sets the total number of results in all pages.
        /// </summary>
        public int Total
        {
            get
            {
                return m_Total;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Total));
                m_Total = value;
            }
        }

        public ResultsPage(int capacity) : base(capacity) { }
    }
}