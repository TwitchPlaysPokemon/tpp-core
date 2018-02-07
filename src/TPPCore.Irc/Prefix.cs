using System.Diagnostics;

namespace TPPCore.Irc
{
    /// <summary>
    /// Representation of a server or a user.
    /// </summary>
    /// <remarks>
    /// A server is usually a hostname such as <code>irc.example.com</code>
    /// or a client identifer.
    /// </remarks>
    public class Prefix
    {
        /// <summary>
        /// Raw string of the prefix (without colon prefix) from
        /// <see cref="ParseFrom"/>.
        /// </summary>
        public string Raw;

        /// <summary>
        /// Client identifier.
        /// </summary>
        /// <remarks>
        /// Value will be null if it is not a client.
        /// </remarks>
        public ClientId ClientId;

        /// <summary>
        /// Server name.
        /// </summary>
        /// <remarks>
        /// Value will be null if it is not a server.
        /// </remarks>
        public string Server;

        /// <summary>
        /// Populate from a raw string.
        /// </summary>
        public void ParseFrom(string part)
        {
            Debug.Assert(!part.StartsWith(":"));

            Raw = part;

            var candidateClientId = new ClientId();
            candidateClientId.ParseFrom(part);

            if (candidateClientId.Host != null)
            {
                ClientId = candidateClientId;
            }
            else
            {
                Server = part;
            }
        }

        override public string ToString()
        {
            if (ClientId != null)
            {
                return ClientId.ToString();
            }
            else if (Server != null)
            {
                return Server;
            }
            else if (Raw != null)
            {
                return Raw;
            }
            else
            {
                return "";
            }
        }
    }
}
