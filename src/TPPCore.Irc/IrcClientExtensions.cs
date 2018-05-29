using System;
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
        /// Send string command and parameters.
        /// </summary>
        /// <remarks>
        /// This is a convenience method to skip creating a new message object.
        /// To send a tailing parameter, use <see cref="SendParamsTrailing"/>.
        /// </remarks>
        public static async Task SendParams(this IrcClient client,
        string command, params string[] parameters)
        {
            await client.SendMessage(new Message(command, parameters));
        }

        /// <summary>
        /// Send string command and parameters with trailing parameter.
        /// </summary>
        /// <remarks>
        /// This is a convenience method to skip creating a new message object.
        /// To send without a trailing parameter <see cref="SendParams"/>.
        /// </remarks>
        public static async Task SendParamsTrailing(this IrcClient client,
        string command, params string[] parameters)
        {
            string[] leadingParams = null;
            String trailingParam = null;

            if (parameters.Length > 0) {
                var leadingCount = parameters.Length - 1;
                leadingParams = parameters.Take(leadingCount).ToArray();
                trailingParam = parameters.Last();
            }

            await client.SendMessage(
                new Message(command, leadingParams, trailingParam));
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
                await client.SendParams("PASS", password);
            }
            await client.SendParams("NICK", nickname);
            await client.SendParamsTrailing("USER", username, "8", "*", realName);
        }

        /// <summary>
        /// Joins specified channels.
        /// </summary>
        public static async Task Join(this IrcClient client,
        params string[] channels)
        {
            await client.SendParams("JOIN", string.Join(",", channels));
        }

        /// <summary>
        /// Joins specified channel with a password.
        /// </summary>
        public static async Task Join(this IrcClient client, string channel,
        string key)
        {
            await client.SendParams("JOIN", channel, key);
        }

        /// <summary>
        /// Parts specified channels.
        /// </summary>
        public static async Task Part(this IrcClient client,
        params string[] channels)
        {
            await client.SendParams("PART", string.Join(",", channels));
        }

        /// <summary>
        /// Parts specified channel with a goodbye message.
        /// </summary>
        public static async Task Part(this IrcClient client, string channel,
        string partMessage)
        {
            await client.SendParamsTrailing("PART", channel, partMessage);
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
            await client.SendParamsTrailing("NOTICE", destination, text);
        }
    }
}
