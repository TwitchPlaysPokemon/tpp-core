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
        /// Gets the topic for the pub-sub message.
        /// </summary>
        public abstract Topic GetTopic();
    }
}
