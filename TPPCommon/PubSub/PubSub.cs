using System;
using System.Collections.Generic;

namespace TPPCommon.PubSub
{
    public class PubSub : IPubSub
    {
        private interface EventHandlerWrapper
        {
            void InvokeIfApplicable(IEvent @event);
        }

        private class EventHandlerWrapper<T> : EventHandlerWrapper where T : IEvent
        {
            private PubSubEventHandler<T> _handler;

            public EventHandlerWrapper(PubSubEventHandler<T> handler)
            {
                _handler = handler;
            }

            public void InvokeIfApplicable(IEvent @event)
            {
                if (@event is T fittingEvent)
                {
                    _handler.Invoke(fittingEvent);
                }
            }
        }

        private readonly IList<EventHandlerWrapper> _eventHandlerWrappers = new List<EventHandlerWrapper>();
        private IList<IEvent> eventQueue = new List<IEvent>();
        private bool isProcessingQueue;
        private readonly object syncLock = new Object();

        private void processQueue()
        {
            lock (syncLock)
            {
                isProcessingQueue = true;
                var eventsToProcess = eventQueue;
                eventQueue = new List<IEvent>();
                foreach (var @event in eventsToProcess)
                {
                    foreach (var eventHandlerWrapper in _eventHandlerWrappers)
                    {
                        eventHandlerWrapper.InvokeIfApplicable(@event);
                    }
                }
                eventQueue.Clear();
                isProcessingQueue = false;
            }
            if (!isProcessingQueue && eventQueue.Count > 0)
            {
                processQueue();
            }
        }

        public void Publish(IEvent @event)
        {
            eventQueue.Add(@event);
            if (!isProcessingQueue)
            {
                processQueue();
            }
        }

        public void Subscribe<T>(PubSubEventHandler<T> handler) where T : IEvent
        {
            // TODO handle concurrent subscriptions
            var wrapper = new EventHandlerWrapper<T>(handler);
            _eventHandlerWrappers.Add(wrapper);
        }
    }
}