using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// Represents a post request to be sent by a Restful client to a Restful server.
    /// </summary>
    public class PostObject : IRestEvent
    {
        /// <summary>
        /// Website or server endpoint name such as IRC or Twitch.
        /// </summary>
        public string ClientName;

        public virtual JObject ToJObject()
        {
            Debug.Assert(ClientName != null);

            return JObject.FromObject(new
            {
                clientName = ClientName,
            });
        }
    }
}
