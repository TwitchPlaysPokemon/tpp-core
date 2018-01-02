namespace TPPCore.Service.Common
{
    public interface IPubSubClient
    {
        void Publish(string topic, string message);
    }
}
