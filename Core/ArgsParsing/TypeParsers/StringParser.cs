using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Core.ArgsParsing.TypeParsers
{
    public class StringParser : BaseArgumentParser<string>
    {
        public override Task<ArgsParseResult<string>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes)
        {
            var result = ArgsParseResult<string>.Success(args.First(), args.Skip(1).ToImmutableList());
            return Task.FromResult(result);
        }
    }
}
