using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TPPCore.Irc
{
    /// <summary>
    /// Tracks channels and users.
    /// </summary>
    /// <remarks>
    /// The keys are case-insensitive.
    /// </remarks>
    public class ChannelTracker : Dictionary<string,ChannelRoom>
    {
        public ChannelTracker()
        : base(new IrcCaseInsensitiveStringEqualityComparer())
        {
        }

        /// <summary>
        /// Update from the given message.
        /// </summary>
        public void UpdateFromMessage(Message message)
        {
            if (message.NumericReply >= 0)
            {
                updateByNumericReply(message);
                return;
            }

            var clientId = message.Prefix.ClientId;

            if (clientId == null)
            {
                return;
            }

            updateByClientId(message.Command, clientId);

            var target = message.TargetLower;

            if (target == null)
            {
                return;
            }

            updateByClientIdTarget(message.Command, clientId, target);

            if (!target.IsChannel())
            {
                return;
            }

            var channelName = target;
            var channel = getOrCreateChannel(channelName);

            updateByChannel(message, channel, clientId);
        }

        private ChannelRoom getOrCreateChannel(string channelName)
        {
            if (!this.ContainsKey(channelName))
            {
                this[channelName] = new ChannelRoom(channelName);
            }

            return this[channelName];
        }

        private void updateByClientId(string command, ClientId clientId)
        {
            switch (command)
            {
                case "QUIT":
                    removeAllNickname(clientId.NicknameLower);
                    break;
            }
        }

        private void updateByClientIdTarget(string command, ClientId clientId,
        string target)
        {
            switch (command)
            {
                case "NICK":
                    renameNickname(clientId.NicknameLower, target);
                    break;
            }
        }

        private void updateByChannel(Message message, ChannelRoom channel,
        ClientId clientId)
        {
            switch (message.Command)
            {
                case "JOIN":
                    channel.AddOrIgnoreClientId(clientId);
                    break;
                case "PART":
                    channel.Remove(clientId.NicknameLower);
                    break;
                case "KICK":
                    if (message.Parameters.Count >= 2)
                    {
                        channel.Remove(message.Parameters[1]);
                    }
                    break;
            }
        }

        private void updateByNumericReply(Message message)
        {
            switch (message.NumericReply)
            {
                case NumericalReplyCodes.RPL_NAMREPLY:
                    if (message.Parameters.Count == 4)
                    {
                        var nameReply = ReplyParser.ParseNameReply(message);
                        var channel = getOrCreateChannel(nameReply.Channel);
                        channel.UpdateByNameReply(nameReply);
                    }
                    break;
            }
        }

        private void removeAllNickname(string nickname)
        {
            foreach (var channel in this.Values)
            {
                channel.Remove(nickname);
            }
        }

        private void renameNickname(string from, string to)
        {
            foreach (var channel in this.Values)
            {
                if (channel.ContainsKey(from))
                {
                    channel[to] = channel[from];
                    channel.Remove(from);
                    channel[to].Rename(to);
                }
            }
        }
    }
}
