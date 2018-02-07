using System.Text.RegularExpressions;

namespace TPPCore.Irc
{
    /// <summary>
    /// Helper functions for parsing strings.
    /// </summary>
    public static class ParserStringExtensions
    {
        private static Regex spaceSplitRegex = new Regex(@"([^ ]+) +(.*)", RegexOptions.Compiled);

        /// <summary>
        /// Splits a string into two parts separated by a space or spaces.
        /// </summary>
        /// <param name="First">String before the space.</param>
        /// <param name="Remainder">String after the space. Empty string if no space.</param>
        public static (string First, string Remainder) SplitSpace(this string input)
        {
            var match = spaceSplitRegex.Match(input);

            if (match.Success)
            {
                return (
                    First: match.Groups[1].Value,
                    Remainder: match.Groups[2].Value
                );
            }
            else
            {
                return (First: input, Remainder: "");
            }
        }
    }
}
