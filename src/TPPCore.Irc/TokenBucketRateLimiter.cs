using System;
using System.Diagnostics;

namespace TPPCore.Irc
{
    public class TokenBucketRateLimiter : IRateLimiter
    {
        private readonly int maxTokens;
        private readonly int period;
        private readonly int minDelay;
        private int numTokens;
        private Stopwatch stopwatch;
        private long incrementTimestamp = 0;
        private long tokenTimestamp = 0;

        /// <param name="maxTokens">Maximum number of messages that can be
        /// sent in a period ("burst").</param>
        /// <param name="period">Time in milliseconds of the period.</param>
        /// <param name="minDelay">Minimum time in milliseconds between
        /// each message.</param>
        public TokenBucketRateLimiter(int maxTokens, int period,
        int minDelay = 200)
        {
            Debug.Assert(maxTokens > 0);
            Debug.Assert(period > 0);
            Debug.Assert(minDelay >= 0);

            this.maxTokens = this.numTokens = maxTokens;
            this.period = period;
            this.minDelay = minDelay;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public int GetWaitTime()
        {
            var timestamp = stopwatch.ElapsedMilliseconds;

            if (numTokens == maxTokens && tokenTimestamp == 0)
            {
                tokenTimestamp = timestamp;
            }

            while (tokenTimestamp < timestamp)
            {
                numTokens = Math.Min(maxTokens, numTokens + 1);
                tokenTimestamp += period;
            }

            var delay = 0;

            if (numTokens <= 0)
            {
                delay = (int) (tokenTimestamp - timestamp);
            }

            var timeBetweenIncrement = timestamp - incrementTimestamp;

            if (delay < minDelay && timeBetweenIncrement < minDelay)
            {
                delay += minDelay - (int) timeBetweenIncrement;
            }

            return delay;
        }

        public void Increment()
        {
            if (numTokens > 0)
            {
                numTokens -= 1;
                incrementTimestamp = stopwatch.ElapsedMilliseconds;
            }
        }
    }
}
