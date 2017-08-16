using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace TPPCommon.PubSub.Messages
{
    /// <summary>
    /// Abstract container to represent a strongly-typed pub-sub message.
    /// </summary>
    [DataContract]
    public abstract class PubSubMessage
    {
        /// <summary>
        /// Returns the topic for the pub-sub message class.
        /// If the class has the Topic attribute, it uses that.
        /// Otherwise the class name is used.
        /// </summary>
        public static Topic GetTopicForMessageType(Type type)
        {
            var isSubclass = typeof(PubSubMessage).GetTypeInfo().IsAssignableFrom(type);
            if (!isSubclass)
            {
                throw new ArgumentException("event type must be subclass of " + typeof(PubSubMessage));
            }
            var topicAttribute = type.GetTypeInfo().GetCustomAttribute<TopicAttribute>();
            if (topicAttribute == null)
            {
                throw new ArgumentException("the class" + type + " must define a topic with the " + typeof(TopicAttribute) + " attribute.");
            }
            return topicAttribute.Topic;
        }
        
        /// <summary>
        /// Returns the topic for the pub-sub message.
        /// See also <see cref="GetTopicForMessageType"/>
        /// </summary>
        public Topic GetTopic()
        {
            return GetTopicForMessageType(this.GetType());
        }
    }
}
