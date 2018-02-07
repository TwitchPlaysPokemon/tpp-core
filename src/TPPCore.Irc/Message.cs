using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TPPCore.Irc
{
    /// <summary>
    /// IRC message representation.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc2812 for specifications.
    /// </remarks>
    public class Message
    {
        /// <summary>
        /// Raw line without the trailing newlines from <see cref="ParseFrom"/>.
        /// </summary>
        public string Raw;

        /// <summary>
        /// IRCv3 tag collection.
        /// </summary>
        public Tags Tags;

        /// <summary>
        /// Source of the message.
        /// </summary>
        public Prefix Prefix;

        /// <summary>
        /// IRC command.
        /// </summary>
        /// <remarks>
        /// This value is typically uppercase or null if a numerical reply.
        /// Parsing converts this value to uppercase. Formatting to string
        /// does not change this value's casing.
        /// </remarks>
        public string Command;

        /// <summary>
        /// A numerical reply code.
        /// </summary>
        /// <remarks>
        /// This value is -1 if a command is specified.
        /// </remarks>
        public int NumericReply = -1;

        /// <summary>
        /// Parameters of a command or numerical reply.
        /// </summary>
        /// <remarks>
        /// All parameters except the last one must not have a space.
        /// </remarks>
        public List<string> Parameters;

        /// <summary>
        /// Whether the last parameter is a trailing parameter that may have
        /// spaces.
        /// </summary>
        public bool HasTrailing = false;

        /// <summary>
        /// The first parameter.
        /// </summary>
        /// <remarks>
        /// <para>This value is null if there is no parameters.
        /// This value may be the same as the trailing parameter.</para>
        /// <para>Most messages have the first parameter as a target which
        /// maybe a nickname, a channel, an IP address, a server, a host-mask
        /// or a comma-separated list.</para>
        /// <para>In most cases, it's either a channel or a nickname.
        /// If you need to compare channels or nicknames for equivalency,
        /// use <see cref="TargetLower"/>.</para>
        /// </remarks>
        public string Target
        {
            get
            {
                if (Parameters.Any())
                {
                    return Parameters[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Lowercase version of <see cref="Target"/>.
        /// </summary>
        public string TargetLower
        {
            get
            {
                return Target != null ? Target.ToLowerIrc() : null;
            }
        }

        /// <summary>
        /// The last parameter which may contain spaces.
        /// </summary>
        /// <remarks>
        /// The value is null if there is no trailing parameter.
        /// </remarks>
        public string TrailingParameter
        {
            get
            {
                return HasTrailing && Parameters.Any()
                    ? Parameters.Last() : null;
            }
        }

        /// <summary>
        /// CTCP message of the trailing parameter.
        /// </summary>
        /// <remarks>
        /// The value is null if there is no CTCP message or trailing parameter.
        /// </remarks>
        public CtcpMessage CtcpMessage
        {
            get
            {
                if (HasTrailing && Parameters.Any()
                && Parameters.Last().StartsWith("\u0001"))
                {
                    var ctcpMessage = new CtcpMessage();
                    ctcpMessage.ParseFrom(TrailingParameter);
                    return ctcpMessage;
                }
                else
                {
                    return null;
                }
            }
        }

        public Message()
        {
            Tags = new Tags();
            Prefix = new Prefix();
            Parameters = new List<string>();
        }

        public Message(string command, string[] parameters = null,
        string trailingParameter = null)
        : this()
        {
            this.Command = command;

            if (parameters != null)
            {
                this.Parameters = new List<string>(parameters);
            }

            if (trailingParameter != null)
            {
                HasTrailing = true;
                this.Parameters.Add(trailingParameter);
            }
        }

        public Message(string command, string[] parameters,
        CtcpMessage ctcpMessage)
        : this(command, parameters, ctcpMessage.ToString())
        {
        }

        public Message(int numericalReplyCode, string[] parameters,
        string trailingParameter = null)
        : this(null, parameters, trailingParameter)
        {
            this.NumericReply = numericalReplyCode;
        }

        /// <summary>
        /// Populate from a line of IRC.
        /// </summary>
        /// <param name="line">Raw IRC line without trailing newlines.</param>
        public void ParseFrom(string line)
        {
            Debug.Assert(!line.EndsWith("\n"));
            Debug.Assert(!line.EndsWith("\r\n"));

            Raw = line;
            var remainder = line;

            if (remainder.StartsWith("@"))
            {
                var (part, splitRemainder) = remainder.SplitSpace();
                remainder = splitRemainder;

                Tags.ParseFrom(part.Substring(1));
            }

            if (remainder.StartsWith(":"))
            {
                var (part, splitRemainder) = remainder.SplitSpace();
                remainder = splitRemainder;

                Prefix.ParseFrom(part.Substring(1));
            }

            if (remainder.Length == 0)
            {
                throw new IrcParserException("Missing command or numerical reply.");
            }

            {
                var (part, splitRemainder) = remainder.SplitSpace();
                remainder = splitRemainder;

                if (!int.TryParse(part, out NumericReply)) {
                    Command = part.ToUpperInvariant();
                }
            }

            while (remainder.Length > 0)
            {
                if (remainder.StartsWith(":"))
                {
                    Parameters.Add(remainder.Substring(1));
                    HasTrailing = true;
                    break;
                }

                var (part, splitRemainder) = remainder.SplitSpace();
                remainder = splitRemainder;

                Parameters.Add(part);
            }
        }

        override public string ToString()
        {
            var builder = new StringBuilder();

            if (Tags.Any())
            {
                builder.Append("@");
                builder.Append(Tags.ToString());
                builder.Append(" ");
            }

            var prefixString = Prefix.ToString();
            if (prefixString.Length > 0) {
                builder.Append(":");
                builder.Append(prefixString);
                builder.Append(" ");
            }

            if (Command != null)
            {
                builder.Append(Command);
            }
            else
            {
                builder.Append(NumericReply);
            }

            foreach (var index in Enumerable.Range(0, Parameters.Count))
            {
                var parameter = Parameters[index];
                builder.Append(" ");

                if (index == Parameters.Count - 1 && HasTrailing)
                {
                    builder.Append(":");
                }
                else
                {
                    Debug.Assert(parameter != null);
                    Debug.Assert(!parameter.Contains(' '));
                }

                builder.Append(parameter);
            }

            var output = builder.ToString();
            output.CheckUnsafeChars();
            return output;
        }
    }
}
