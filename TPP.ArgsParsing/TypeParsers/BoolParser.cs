using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace TPP.ArgsParsing.TypeParsers;

public class BoolParser : IArgumentParser<bool>
{
    public Task<ArgsParseResult<bool>> Parse(IImmutableList<string> args, Type[] genericTypes) =>
        Task.FromResult(args[0].ToLower() switch
        {
            "y" or "yes" or "true" => ArgsParseResult<bool>.Success(true, args.Skip(1).ToImmutableList()),
            "n" or "no" or "false" => ArgsParseResult<bool>.Success(false, args.Skip(1).ToImmutableList()),
            _ => ArgsParseResult<bool>.Failure($"Did not recognize '{args[0]}' as a boolean")
        });
}
