namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// Unparsed message from an endpoint.
    /// </summary>
    public class RawContentEvent : ChatEvent
    {
        public string RawContent;

        public RawContentEvent() : base(ChatTopics.Raw)
        {
        }
    }
}
