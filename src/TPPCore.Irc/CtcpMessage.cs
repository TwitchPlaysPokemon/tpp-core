namespace TPPCore.Irc
{
    /// <summary>
    /// Represents CTCP messages found in PRIVMSG and NOTICE messages.
    /// </summary>
    /// <remarks>
    /// CTCP messages are often known as /me commands or file transfer
    /// commands. For security reasons, this class only recognizes one
    /// CTCP message per line.
    /// See https://tools.ietf.org/html/draft-oakley-irc-ctcp-02 for
    /// the spec.
    /// </remarks>
    public class CtcpMessage
    {
        /// <summary>
        /// CTCP command.
        /// </summary>
        /// <remarks>
        /// Commands are case-insensitive and parsing converts this value
        /// to uppercase. Formatting to string does not change this values's
        /// casing.
        /// </remarks>
        public string Command;

        /// <summary>
        /// CTCP parameters.
        /// </summary>
        /// <remarks>
        /// When there is no parameter, this value is an empty string.
        /// </remarks>
        public string Parameters;

        public CtcpMessage()
        {
        }

        public CtcpMessage(string command, string parameters = "")
        {
            this.Command = command;
            this.Parameters = parameters;
        }

        /// <summary>
        /// Populate from a raw string.
        /// </summary>
        /// <param name="part">The text from a PRIVMSG or NOTICE with
        /// optional delimiters.</param>
        public void ParseFrom(string message)
        {
            message = message.Trim('\u0001');

            var result = message.Split(new[] {' '}, 2);
            Command = result[0].ToUpperInvariant();

            if (result.Length > 1)
            {
                Parameters = result[1];
            }
            else
            {
                Parameters = "";
            }
        }

        override public string ToString()
        {
            return ToString(Command == "ACTION");
        }

        public string ToString(bool withSpaceOnEmptyParam)
        {
            if (Parameters != null && Parameters.Length > 0 || withSpaceOnEmptyParam)
            {
                return $"\u0001{Command} {Parameters}\u0001";
            }
            else
            {
                return $"\u0001{Command}\u0001";
            }
        }
    }
}
