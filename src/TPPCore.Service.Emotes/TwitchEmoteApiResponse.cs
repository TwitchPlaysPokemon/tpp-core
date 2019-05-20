using System.Collections.Generic;

namespace TPPCore.Service.Emotes
{
    public class TwitchEmoteApiResponse
    {
        /// <summary>
        /// The list of the emotes.
        /// </summary>
        public List<Emote> emoticons;

        public class Emote
        {
            /// <summary>
            /// The ID of the emote.
            /// </summary>
            public string id;

            /// <summary>
            /// The regex of the emote.
            /// </summary>
            public string code;
        }
    }
}
