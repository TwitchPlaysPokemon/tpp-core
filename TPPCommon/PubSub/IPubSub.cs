namespace TPPCommon.PubSub
{
    public delegate void PubSubEventHandler<T>(T @event) where T : IEvent;
    
    public interface IPubSub
    {
        void Publish(IEvent @event);
        void Subscribe<T>(PubSubEventHandler<T> handler) where T : IEvent;
    }
}
