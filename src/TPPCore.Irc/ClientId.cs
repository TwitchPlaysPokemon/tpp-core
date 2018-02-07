using System;
using System.Text.RegularExpressions;

namespace TPPCore.Irc
{
    /// <summary>
    /// Representation of a user's unique client identifer.
    /// </summary>
    public class ClientId : IEquatable<ClientId>
    {
        /// <summary>
        /// A user's chosen identifer.
        /// </summary>
        /// <remarks>
        /// A nickname is IRC-style case insensitive. This value contains
        /// the original casing. If you need to compare nicknames, use
        /// <see cref="NicknameLower"/>.
        /// </remarks>
        public string Nickname;

        /// <summary>
        /// Lowercase version of nickname.
        /// </summary>
        public string NicknameLower { get { return Nickname.ToLowerIrc(); }}

        /// <summary>
        /// A user's username.
        /// </summary>
        /// <remarks>
        /// This value is sometimes prefixed with <code>~</code> when a server
        /// cannot verify a client's username using an ident service.
        /// </remarks>
        public string User;

        /// <summary>
        /// A user's hostname or IP address.
        /// </summary>
        public string Host;

        private Regex parseRegex = new Regex(@"([^!]+)!([^@]+)@(.*)", RegexOptions.Compiled);

        public ClientId()
        {
        }

        public ClientId(string nickname)
        {
            this.Nickname = nickname;
        }

        public ClientId(string nickname, string user, string host)
        {
            this.Nickname = nickname;
            this.User = user;
            this.Host = host;
        }

        public bool NicknameEquals(ClientId other)
        {
            if (other == null)
            {
                return false;
            }

            return NicknameLower == other.NicknameLower;
        }

        public bool Equals(ClientId other)
        {
            if (other == null)
            {
                return false;
            }

            return NicknameLower == other.NicknameLower
                && User == other.User
                && Host == other.Host;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null) {
                return false;
            }

            var other = obj as ClientId;

            if (other == null) {
                return false;
            }

            return Equals(other);
        }

        public static bool operator == (ClientId clientId1, ClientId clientId2)
        {
            if (((object) clientId1) == null || ((object) clientId2) == null) {
                return Object.Equals(clientId1, clientId2);
            }
            return clientId1.Equals(clientId2);
        }

        public static bool operator != (ClientId clientId1, ClientId clientId2)
        {
            if (((object) clientId1) == null || ((object) clientId2) == null) {
                return !Object.Equals(clientId1, clientId2);
            }
            return !clientId1.Equals(clientId2);
        }

        public override int GetHashCode()
        {
            if (User == null || Host == null)
            {
                return Nickname.GetHashCode();
            }

            unchecked
            {
                int hash = 17;
                hash = hash * 23 + NicknameLower.GetHashCode();
                hash = hash * 23 + User.GetHashCode();
                hash = hash * 23 + Host.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Populate from a raw string.
        /// </summary>
        public void ParseFrom(string part)
        {
            var match = parseRegex.Match(part);

            if (match.Success)
            {
                Nickname = match.Groups[1].Value;
                User = match.Groups[2].Value;
                Host = match.Groups[3].Value;
            }
            else
            {
                Nickname = part;
            }
        }

        override public string ToString()
        {
            if (User != null && Host != null)
            {
                return $"{Nickname}!{User}@{Host}";
            }
            else
            {
                return Nickname;
            }
        }
    }
}
