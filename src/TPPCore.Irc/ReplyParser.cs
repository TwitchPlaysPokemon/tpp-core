using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TPPCore.Irc
{
    /// <summary>
    /// Nicknames for a channel from a RPL_NAMREPLY.
    /// </summary>
    public struct NameReply
    {
        public string Visibility;
        public string Channel;
        public IList<NameReplyItem> Names;

        public NameReply(string visibility, string channel)
        {
            this.Visibility = visibility;
            this.Channel = channel;
            this.Names = new List<NameReplyItem>();
        }
    }

    /// <summary>
    /// A nickname in a RPL_NAMREPLY.
    /// </summary>
    public struct NameReplyItem
    {
        public string Nickname;
        public string NicknamePrefix;

        public NameReplyItem(string nicknamePrefix, string nickname)
        {
            this.Nickname = nickname;
            this.NicknamePrefix = nicknamePrefix;
        }
    }

    public class ReplyParser
    {
        /// <summary>
        /// Parses a RPL_NAMREPLY reply.
        /// </summary>
        public static NameReply ParseNameReply(Message message)
        {
            Debug.Assert(message.NumericReply == NumericalReplyCodes.RPL_NAMREPLY);
            Debug.Assert(message.Parameters.Count == 4);

            var nameReply = new NameReply(message.Parameters[1], message.Parameters[2]);

            foreach (var item in message.TrailingParameter.Split(new[] {' '}))
            {
                var match = Regex.Match(item, @"([@+]?)(.+)");

                if (match.Success)
                {
                    var nameItem = new NameReplyItem(match.Groups[1].Value, match.Groups[2].Value);
                    nameReply.Names.Add(nameItem);
                }
            }

            return nameReply;
        }
    }
}
