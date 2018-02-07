using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TPPCore.Irc
{
    /// <summary>
    /// Methods to send common messages easily.
    /// </summary>
    public static class IrcClientExtensions
    {
        /// <summary>
        /// Convenience method to skip creating a new message object.
        /// </summary>
        /// <remarks>
        /// To indicate that the last parameter is a trailing parameter,
        /// separate it by passing null between the parameters and the trailing
        /// parameters.
        /// </remarks>
        public static async Task SendMessage(this IrcClient client,
        string command, params string[] parameters)
        {
            var leadingParams = new List<string>();
            string trailingParam = null;
            var hasTrailing = false;

            foreach (var part in parameters)
            {
                if (hasTrailing)
                {
                    Debug.Assert(trailingParam == null);
                    trailingParam = part;
                }
                else if (part != null)
                {
                    leadingParams.Add(part);
                }
                else
                {
                    hasTrailing = true;
                }
            }

            await client.SendMessage(new Message(
                command, leadingParams.ToArray(), trailingParam));
        }

        /// <summary>
        /// Keep processing messages until a message with a reply code is met.
        /// </summary>
        /// <remarks>
        /// This will call <see cref="IrcClient.ProcessOnce"/> repeatedly.
        /// </remarks>
        /// <param name="timeout">Time in milliseconds.</param>
        public static async Task WaitReply(this IrcClient client,
        int numericReplyCode, int timeout = 0)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (true)
            {
                var message = await client.ProcessOnce(timeout);

                if (message.NumericReply == numericReplyCode)
                {
                    break;
                }

                if (timeout > 0 && stopwatch.ElapsedMilliseconds > timeout)
                {
                    throw new IrcTimeoutException();
                }
            }
        }

        /// <summary>
        /// Send login information.
        /// </summary>
        public static async Task Register(this IrcClient client,
        string nickname, string username,
        string realName, string password = null)
        {
            if (password != null)
            {
                await client.SendMessage("PASS", password);
            }
            await client.SendMessage("NICK", nickname);
            await client.SendMessage("USER", username, "8", "*", null, realName);
        }

        /// <summary>
        /// Joins specified channels.
        /// </summary>
        public static async Task Join(this IrcClient client,
        params string[] channels)
        {
            await client.SendMessage("JOIN", string.Join(",", channels));
        }

        /// <summary>
        /// Joins specified channel with a password.
        /// </summary>
        public static async Task Join(this IrcClient client, string channel,
        string key)
        {
            await client.SendMessage("JOIN", channel, key);
        }

        /// <summary>
        /// Parts specified channels.
        /// </summary>
        public static async Task Part(this IrcClient client,
        params string[] channels)
        {
            await client.SendMessage("PART", string.Join(",", channels));
        }

        /// <summary>
        /// Parts specified channel with a goodbye message.
        /// </summary>
        public static async Task Part(this IrcClient client, string channel,
        string partMessage)
        {
            await client.SendMessage("PART", channel, null, partMessage);
        }

        /// <summary>
        /// Sends a PRIVMSG.
        /// </summary>
        public static async Task Privmsg(this IrcClient client,
        string destination, string text, bool action = false)
        {
            Message message;

            if (action)
            {
                message = new Message("PRIVMSG", new[] { destination },
                    new CtcpMessage("ACTION", text));
            }
            else
            {
                message = new Message("PRIVMSG", new[] { destination }, text);
            }

            await client.SendMessage(message);
        }

        /// <summary>
        /// Sends a NOTICE.
        /// </summary>
        public static async Task Notice(this IrcClient client,
        string destination, string text)
        {
            await client.SendMessage("NOTICE", destination, null, text);
        }
    }
}
