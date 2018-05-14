using System;
using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// A user's chat message.
    /// </summary>
    public class ChatMessage : ChatEvent
    {
        public ChatMessage() : base(ChatTopics.Message)
        {
        }
        /// <summary>
        /// Who sent this message.
        /// </summary>
        public ChatUser Sender;

        /// <summary>
        /// Message contents.
        /// </summary>
        public string TextContent;

        /// <summary>
        /// Originating chat room.
        /// </summary>
        /// <remarks>
        /// This value will be null if the message did not originate in a room,
        /// ie, a private message or whisper.
        /// </remarks>
        public string Channel;

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
        /// Whether the message is an IRC-style notice message.
        /// </summary>
        public bool IsNotice = false;

    }
}
