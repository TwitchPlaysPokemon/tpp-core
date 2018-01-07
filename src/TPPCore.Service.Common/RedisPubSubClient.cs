using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace TPPCore.Service.Common
{
    public class RedisPubSubClient : IPubSubClient, IDisposable
    {
        private ConnectionMultiplexer redis;
        private Dictionary<(string Topic,Action<string,string> Handler),Action<RedisChannel,RedisValue>> handlers;

        public RedisPubSubClient(string host = "localhost", int port = 6379, string extra = "")
        {
            redis = ConnectionMultiplexer.Connect($"{host}:{port}," + extra);
            handlers = new Dictionary<(string Topic,Action<string,string> Handler),Action<RedisChannel,RedisValue>>();
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

        public void Subscribe(string topic, Action<string, string> handler)
        {
            var handlerKey = (Topic: topic, Handler: handler);

            if (handlers.ContainsKey(handlerKey))
            {
                return;
            }

            ISubscriber sub = redis.GetSubscriber();
            Action<RedisChannel,RedisValue> redisHandler =
                (redisChannel, redisValue) =>
                {
                    handler(redisChannel.ToString(), redisValue.ToString());
                };

            sub.Subscribe(topic, redisHandler);
            handlers.Add(handlerKey, redisHandler);
        }

        public void Unsubscribe(string topic, Action<string, string> handler)
        {
            var handlerKey = (Topic: topic, Handler: handler);

            if (handlers.ContainsKey(handlerKey))
            {
                var redisHandler = handlers[handlerKey];
                ISubscriber sub = redis.GetSubscriber();
                sub.Unsubscribe(topic, redisHandler);
                handlers.Remove(handlerKey);
            }
        }
    }
}
