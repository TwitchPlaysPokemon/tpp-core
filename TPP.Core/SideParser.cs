using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing;
using TPP.Match;

namespace TPP.Core
{
    public class SideParser : BaseArgumentParser<Side>
    {
        public override Task<ArgsParseResult<Side>> Parse(IImmutableList<string> args, Type[] genericTypes) =>
            Task.FromResult(args[0].ToLower() switch
            {
                "blue" => ArgsParseResult<Side>.Success(Side.Blue, args.Skip(1).ToImmutableList()),
                "red" => ArgsParseResult<Side>.Success(Side.Red, args.Skip(1).ToImmutableList()),
                _ => ArgsParseResult<Side>.Failure($"invalid side '{args[0]}'")
            });
    }
}
