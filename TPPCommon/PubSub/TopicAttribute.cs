namespace TPPCommon.PubSub
{
    [System.AttributeUsage(System.AttributeTargets.Class)] 
    public class TopicAttribute : System.Attribute
    {
        public Topic Topic { get; }

        public TopicAttribute(Topic topic)
        {
            Topic = topic;
        }
    }
}