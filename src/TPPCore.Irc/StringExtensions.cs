namespace TPPCore.Irc
{
    public static class StringExtensions
    {
        /// <summary>
        /// Lowercase the string according to IRC specs.
        /// </summary>
        /// <remarks>
        /// This accounts for <code>[]\{}|</code>. It's important to use
        /// this for comparing channel and nicknames.
        /// </remarks>
        public static string ToLowerIrc(this string input)
        {
            // RFC 1459 section 2.2
            return input.ToLowerInvariant()
                .Replace("[", "{")
                .Replace("]", "}")
                .Replace("\\", "|");
        }

        /// <summary>
        /// Returns whether the string contains forbidden characters in
        /// the IRC protocol.
        /// </summary>
        public static bool ContainsUnsafeChars(this string input)
        {
            return input.Contains("\n") || input.Contains("\r");
        }

        public static void CheckUnsafeChars(this string input)
        {
            if (input.ContainsUnsafeChars())
            {
                throw new IrcException("Unsafe characters.");
            }
        }

        /// <summary>
        /// Returns whether the string is formatted as a channel name.
        /// </summary>
        public static bool IsChannel(this string input)
        {
            return input.StartsWith("&")
                || input.StartsWith("#")
                || input.StartsWith("+")
                || input.StartsWith("!");
        }
    }
}
