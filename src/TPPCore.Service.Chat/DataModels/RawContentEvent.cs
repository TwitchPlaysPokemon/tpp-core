using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Chat.DataModels
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

        override public JObject ToJObject()
        {
            var doc = base.ToJObject();

            doc.Add("rawContent", RawContent);

            return doc;
        }
    }
}
