using System;
using System.Collections.Generic;
using System.Text;

namespace TPPCore.ChatProviders.DataModels
{
    public class ChatList
    {
        public List<string> _links;

        /// <summary>
        /// The number of chatters in the room.
        /// </summary>
        public int chatter_count;

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
