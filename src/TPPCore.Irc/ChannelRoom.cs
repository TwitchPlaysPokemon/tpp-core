using System.Collections.Generic;

namespace TPPCore.Irc
{
    /// <summary>
    /// A channel's metadata and mapping of nicknames to users.
    /// </summary>
    /// <remarks>
    /// The keys are case-insensitive.
    /// </remarks>
    public class ChannelRoom : Dictionary<string,ChannelUser>
    {
        /// <summary>
        /// The channel's identifier.
        /// </summary>
        public string Name;

        /// <summary>
        /// Lowercase version of the channel's name.
        /// </summary>
        /// <returns></returns>
        public string NameLower { get { return Name.ToLowerIrc(); } }

        // TODO: keep topic, channel modes, etc

        public ChannelRoom(string name)
        : base(new IrcCaseInsensitiveStringEqualityComparer())
        {
            this.Name = name;
        }

        /// <summary>
        /// Add a ChannelUser from given ClientId if it doesn't exist yet.
        /// </summary>
        public void AddOrIgnoreClientId(ClientId clientId)
        {
            if (!this.ContainsKey(clientId.NicknameLower))
            {
                this[clientId.NicknameLower] = new ChannelUser(clientId);
            }
        }

        public void UpdateByNameReply(NameReply nameReply)
        {
            foreach (var nameItem in nameReply.Names)
            {
                if (!this.ContainsKey(nameItem.Nickname))
                {
                    this[nameItem.Nickname] = new ChannelUser(nameItem.Nickname);
                }
            }
        }
    }
}
