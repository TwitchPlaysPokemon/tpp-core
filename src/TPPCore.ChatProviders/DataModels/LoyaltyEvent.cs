using System;
using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// Represents user initiated events such as subscription, hosting, etc.
    /// </summary>
    public class LoyaltyEvent : ChatEvent
    {
        public LoyaltyEvent() : base(ChatTopics.Loyalty) {
        }

        /// <summary>
        /// Who sent this message.
        /// </summary>
        public ChatUser Sender;

        /// <summary>
        /// Message contents such as a greeting or a textual representation of the event.
        /// </summary>
        /// <remarks>
        /// This value may be null.
        /// </remarks>
        public string TextContent;

        /// <summary>
        /// The emotes in the message.
        /// </summary>
        /// <remarks>
        /// This value may be null.
        /// </remarks>
        public Emotes Emote;

        public class Emotes
        {
            /// <summary>
            /// Contains all the indexes of the emotes in the message.
            /// </summary>
            public List<EmoteOccurance> Ranges;

            /// <summary>
            /// Contains the data, such as the url and the ID of the emotes.
            /// </summary>
            public Dictionary<string, Tuple<int, string>> Data;
        }

        /// <summary>
        /// Chat room where event is occuring.
        /// </summary>
        public string Channel;
    }
}
