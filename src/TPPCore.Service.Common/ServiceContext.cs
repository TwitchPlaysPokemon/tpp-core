namespace TPPCore.Service.Common
{
    public class ServiceContext
    {
        public readonly IPubSubClient pubSubClient;
        // TODO: REST server, config

        public ServiceContext() {
            pubSubClient = new DummyPubSubClient();
        }
    }
}
