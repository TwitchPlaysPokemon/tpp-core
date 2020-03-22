using System;

namespace Core.ArgsParsing
{
    public class ArgsParseFailure : ArgumentException
    {
        public ArgsParseFailure(string? message) : base(message)
        {
        }
    }
}
