using System;
using System.Threading.Tasks;

namespace TPPCommon
{
    /// <summary>
    /// Counter for retry attempt throttling with exponential backoff.
    /// </summary>
    public class ExponentialBackoffCounter {
        public int MinimumBackoffTime {get; set;} = 1_000;
        public int MaximumBackoffTime {get; set;} = 300_000;
        public int CurrentBackoffTime {get; protected set;} = 1_000;
        public bool UsesJitter {get; set;} = true;

        protected Random Random;

        public ExponentialBackoffCounter() {
        }

        public void Reset() {
            CurrentBackoffTime = MinimumBackoffTime;
        }

        public void Increment() {
            CurrentBackoffTime *= 2;
            CurrentBackoffTime = Math.Min(MaximumBackoffTime, CurrentBackoffTime);
        }

        public async Task SleepAsync() {
            int jitter = UsesJitter ? this.Random.Next(1000) : 0;

            await Task.Delay(CurrentBackoffTime + jitter);
        }

        public void Sleep() {
            SleepAsync().Wait();
        }
    }
}
