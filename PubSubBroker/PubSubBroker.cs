using TPPCommon.PubSub;

namespace PubSubBroker
{
    class PubSubBroker
    {
        private IBroker Broker;

        public PubSubBroker(IBroker broker)
        {
            this.Broker = broker;
        }

        public void Run()
        {
            this.Broker.Run();
        }
    }
}
