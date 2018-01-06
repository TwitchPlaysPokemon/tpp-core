using System;
using StackExchange.Redis;

namespace TPPCore.Service.Common
{
    class RedisPubSubClient : IPubSubClient, IDisposable
    {
        private ConnectionMultiplexer redis;

        public RedisPubSubClient(string host = "localhost", int port = 6379)
        {
            redis = ConnectionMultiplexer.Connect($"{host}:{port}");
        }

        public void Dispose()
        {
            redis.Dispose();
        }

        public void Publish(string topic, string message)
        {
            ISubscriber sub = redis.GetSubscriber();
            sub.Publish(topic, message);
        }
    }
}
