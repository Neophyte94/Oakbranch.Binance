using System;
using System.Collections.Generic;

namespace Oakbranch.Binance.Benchmark
{
    public class EndpointSummary
    {
        public readonly string Endpoint;
        public readonly List<TimeSpan> ShortQueryTests = new List<TimeSpan>();
        public readonly List<TimeSpan> MediumQueryTests = new List<TimeSpan>();
        public readonly List<TimeSpan> LongQueryTests = new List<TimeSpan>();

        public TimeSpan? ShortQueryAvgDuration => GetAverageDuration(ShortQueryTests);
        public TimeSpan? MediumQueryAvgDuration => GetAverageDuration(MediumQueryTests);
        public TimeSpan? LongQueryAvgDuration => GetAverageDuration(LongQueryTests);

        public EndpointSummary(string endpoint)
        {
            Endpoint = endpoint;
        }

        private static TimeSpan? GetAverageDuration(List<TimeSpan> tests)
        {
            if (tests == null || tests.Count == 0)
                return null;

            long sum = 0;
            foreach (TimeSpan ts in tests)
            {
                sum += ts.Ticks;
            }

            return new TimeSpan?(new TimeSpan((long)sum / tests.Count));
        }
    }
}
