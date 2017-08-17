using System;

namespace TPPCommon.PubSub
{
    /// <summary>
    /// Raw topic used for pub-sub communication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TopicAttribute : Attribute
    {
        /// <summary>
        /// Required suffix character for all raw topics.
        /// This ensures that no prefix-related issues can occur.
        /// 
        /// The prefix issue occurs when a subscribes subscribes to a topic that is a prefix to another topic. In that case,
        /// the subscriber would receive pub-sub messages from both topics, which is not desirable.
        ///   E.g. "music" is a prefix to "music_pause"
        ///        Therefore, subscribing to the "music" topic also subscribes to the "music_pause" topic.
        ///        Ensuring a unique suffix character guarantees this won't happen.
        /// </summary>
        public const string Suffix = ":";

        public string Topic { get; }

        public TopicAttribute(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic) || topic.IndexOf(Suffix) != -1)
            {
                throw new ArgumentException($"Invalid topic: '{topic}'", nameof(topic));
            }

            Topic = topic + TopicAttribute.Suffix;
        }
    }
}