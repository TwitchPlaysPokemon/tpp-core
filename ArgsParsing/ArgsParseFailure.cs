using System;

namespace ArgsParsing
{
    /// <summary>
    /// Exception thrown by <see cref="ArgsParser"/> if results are being queried without checking for parse failure.
    /// Useful if usages of <see cref="ArgsParser"/> have no reason to handle parsing errors in any specific way and
    /// just want to propagate the error.
    /// </summary>
    public class ArgsParseFailure : ArgumentException
    {
        public ArgsParseFailure(string? message) : base(message)
        {
        }
    }
}
