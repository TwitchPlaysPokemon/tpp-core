using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    public class ChatList
    {
        /// <summary>
        /// The number of chatters in the room.
        /// </summary>
        public int chatter_count;

        /// <summary>
        /// The unformatted list of all the chatters
        /// </summary>
        public Chatters chatters;

        public class Chatters
        {
            /// <summary>
            /// The moderators in the room.
            /// </summary>
            public IEnumerable<string> moderators;

            /// <summary>
            /// The staff in the room.
            /// </summary>
            public IEnumerable<string> staff;

            /// <summary>
            /// The admins in the room.
            /// </summary>
            public IEnumerable<string> admins;

            /// <summary>
            /// The global mods in the room.
            /// </summary>
            public IEnumerable<string> global_mods;

            /// <summary>
            /// The viewers in the room.
            /// </summary>
            public IEnumerable<string> viewers;
        }
    }
}
