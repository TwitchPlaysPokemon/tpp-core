using Newtonsoft.Json.Linq;

namespace TPPCore.ChatProviders
{
    /// <summary>
    /// Represents an object that is used for Rest API queries.
    /// </summary>
    public interface IRestEvent
    {
        JObject ToJObject();
    }
}
