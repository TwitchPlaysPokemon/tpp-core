using System;

namespace TPPCore.Utils
{
    public static class IrcStringExtensions
    {
        public static string ToLowerIrc(this string input)
        {
            // RFC 1459 section 2.2
            return input.ToLowerInvariant()
                .Replace("[", "{")
                .Replace("]", "}")
                .Replace("\\", "|");
        }

        public static bool ContainsUnsafeChars(this string input)
        {
            return input.Contains("\n") || input.Contains("\r");
        }

        public static void CheckUnsafeChars(this string input)
        {
            if (input.ContainsUnsafeChars())
            {
                throw new Exception("Unsafe characters.");
            }
        }
    }
}
