using System;

namespace TPPCore.Service.Common
{
    public interface IPubSubClient
    {
        void Publish(string topic, string message);
        void Subscribe(string topic, Action<string,string> handler);
        void Unsubscribe(string topic, Action<string,string> handler);
    }
}
