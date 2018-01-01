using TPPCommon;
using Xunit;

namespace TPPCommonTest
{
    public class ExponentialBackoffCounterTest
    {
        [Fact]
        public void TestIncrementAndReset()
        {
            var counter = new ExponentialBackoffCounter();

            Assert.Equal(1_000, counter.CurrentBackoffTime);

            counter.Increment();
            Assert.Equal(2_000, counter.CurrentBackoffTime);

            counter.Reset();
            Assert.Equal(1_000, counter.CurrentBackoffTime);
        }

        [Fact]
        public void TestMaximumTime()
        {
            var counter = new ExponentialBackoffCounter();

            for (var index = 0; index < 1000; index++)
            {
                counter.Increment();
            }

            Assert.Equal(300_000, counter.CurrentBackoffTime);
        }
    }
}
