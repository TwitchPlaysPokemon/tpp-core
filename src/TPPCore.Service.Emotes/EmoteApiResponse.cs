using System.Collections.Generic;

namespace TPPCore.Service.Emotes
{
    public class EmoteApiResponse
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
            public int id;

            /// <summary>
            /// The regex of the emote.
            /// </summary>
            public string code;

            /// <summary>
            /// The set that the emoticon belongs in
            /// </summary>
            public int emoticon_set;
        }
    }
}
