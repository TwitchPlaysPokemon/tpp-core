using System.Collections.Generic;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Events;

namespace TPPCommonTest {
    public class MockPubSub : ISubscriber, IPublisher
    {
        public List<PubSubEvent> events;

        public MockPubSub()
        {
            events = new List<PubSubEvent>();
        }

        public void Publish(PubSubEvent @event)
        {
            events.Add(@event);
        }

        public void Subscribe<T>(PubSubEventHandler<T> handler) where T : PubSubEvent
        {
            throw new System.NotImplementedException();
        }
    }
}
