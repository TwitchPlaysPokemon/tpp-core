using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Chat
{
    public interface IPubSubEvent
    {
        string Topic { get; }
        JObject ToJObject();
    }
}
