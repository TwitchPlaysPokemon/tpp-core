using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Common;

namespace TPP.ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser that finds a badge form by name.
    /// </summary>
    public class BadgeFormParser : BaseArgumentParser<int>
    {
        public override Task<ArgsParseResult<int>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string form = args[0];
            ArgsParseResult<int> result;
            try
            {
                result = ArgsParseResult<int>.Success(1, args);
            }
            catch (ArgumentException e)
            {
                result = ArgsParseResult<int>.Failure(e.Message);
            }
            return Task.FromResult(result);
        }
    }
}
