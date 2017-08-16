using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace TPPCommon.PubSub.Events
{
    /// <summary>
    /// Abstract container to represent a strongly-typed pub-sub event.
    /// </summary>
    [DataContract]
    public abstract class PubSubEvent
    {
        /// <summary>
        /// Returns the topic for the pub-sub event class.
        /// If the class has no Topic attribute, an ArgumentException is thrown.
        /// </summary>
        public static Topic GetTopicForEventType(Type type)
        {
            var isSubclass = typeof(PubSubEvent).GetTypeInfo().IsAssignableFrom(type);
            if (!isSubclass)
            {
                throw new ArgumentException("event type must be subclass of " + typeof(PubSubEvent));
            }
            var topicAttribute = type.GetTypeInfo().GetCustomAttribute<TopicAttribute>();
            if (topicAttribute == null)
            {
                throw new ArgumentException("the class" + type + " must define a topic with the " + typeof(TopicAttribute) + " attribute.");
            }
            return topicAttribute.Topic;
        }
        
        /// <summary>
        /// Returns the topic for the pub-sub event.
        /// See also <see cref="GetTopicForEventType"/>
        /// </summary>
        public Topic GetTopic()
        {
            return GetTopicForEventType(this.GetType());
        }
    }
}
