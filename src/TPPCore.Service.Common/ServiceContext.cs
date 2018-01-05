using System.Net;

namespace TPPCore.Service.Common
{
    public class ServiceContext
    {
        public IPubSubClient PubSubClient { get; private set; }
        public RestfulServer RestfulServer { get; private set; }
        // TODO: add way of giving parsed config to service

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

        public void InitRestfulServer(IPAddress host = null, int port = 0)
        {
            host = IPAddress.Loopback ?? host;
            RestfulServer = new RestfulServer(host, port);
        }
    }
}
