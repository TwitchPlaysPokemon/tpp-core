using Newtonsoft.Json.Linq;

namespace TPPCore.ChatProviders
{
    /// <summary>
    /// Represents an object that can be put into a pub/sub system.
    /// </summary>
    public interface IPubSubEvent
    {
        string Topic { get; }
    }
}
