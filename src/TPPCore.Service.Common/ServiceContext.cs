namespace TPPCore.Service.Common
{
    public class ServiceContext
    {
        public IPubSubClient PubSubClient { get; private set; }
        // TODO: REST server, config

        public ServiceContext() {
        }

        public void InitPubSubClient()
        {
            PubSubClient = new DummyPubSubClient();
        }

        public void InitPubSubClient(IPubSubClient pubSubClient)
        {
            PubSubClient = pubSubClient;
        }
    }
}
