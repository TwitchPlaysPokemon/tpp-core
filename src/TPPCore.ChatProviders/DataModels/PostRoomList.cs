using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace TPPCore.ChatProviders.DataModels
{
    /// <summary>
    /// A list of all the users in the chat room.
    /// </summary>
    public class PostRoomList : PostObject
    {
        /// <summary>
        /// The number of users in the room.
        /// </summary>
        public int NumUsers;

        /// <summary>
        /// A list of the viewers in the room.
        /// </summary>
        public List<ChatUser> Viewers = null;

        /// <summary>
        /// A list of the channel moderators in the room.
        /// </summary>
        public List<ChatUser> Moderators = null;

        /// <summary>
        /// A list of the global moderators in the room.
        /// </summary>
        public List<ChatUser> GlobalMods = null;

        /// <summary>
        /// A list of the admins in the room.
        /// </summary>
        public List<ChatUser> Admins = null;

        /// <summary>
        /// A list of the staff in the room.
        /// </summary>
        public List<ChatUser> Staff = null;

        override public JObject ToJObject()
        {
            var doc = base.ToJObject();
            doc.Add("numusers", NumUsers);
            doc.Add("staff", JArray.FromObject(Staff));
            doc.Add("admins", JArray.FromObject(Admins));
            doc.Add("globalmods", JArray.FromObject(GlobalMods));
            doc.Add("mods", JArray.FromObject(Moderators));
            doc.Add("viewers", JArray.FromObject(Viewers));

            return doc;
        }
    }
}
