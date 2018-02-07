namespace TPPCore.Irc
{
    /// <summary>
    /// A user in a channel.
    /// </summary>
    public class ChannelUser
    {
        /// <summary>
        /// Client ID of the user in the channel.
        /// </summary>
        public ClientId ClientId;

        // TODO: track the user's mode

        public ChannelUser(ClientId clientId)
        {
            this.ClientId = clientId;
        }

        public ChannelUser(string nickname)
        {
            ClientId = new ClientId(nickname);
        }

        /// <summary>
        /// Change a user's nickname
        /// </summary>
        public void Rename(string newNickname)
        {
            ClientId.Nickname = newNickname;
        }
    }
}
