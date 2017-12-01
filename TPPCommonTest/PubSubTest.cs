using TPPCommon.PubSub;
using Xunit;

namespace TPPCommonTest
{
    internal class TestEvent : IEvent
    {
    }

    public class PubSubTest
    {
        [Fact]
        public void TestBla()
        {
            var pubsub = new PubSub();
            int called = 0;
            pubsub.Subscribe<TestEvent>(@event => called++);
            pubsub.Publish(new TestEvent());
            Assert.Equal(1, called);
        }
    }
}