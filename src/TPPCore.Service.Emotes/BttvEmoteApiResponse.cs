using System.Collections.Generic;

namespace TPPCore.Service.Emotes
{
    public class BttvEmoteApiResponse
    {
        /// <summary>
        /// The list of the emotes.
        /// </summary>
        public List<Emote> emotes;

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

            /// <summary>
            /// The channel the emote can be used in.
            /// </summary>
            public string channel;
        }
    }
}
