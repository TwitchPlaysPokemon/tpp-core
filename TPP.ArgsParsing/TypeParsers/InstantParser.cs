using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NodaTime.Text;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser capable of parsing instants in strict ISO-8601 UTC format, for example <c>2014-02-12T15:30:00Z</c>.
/// The timezone specifier 'Z' is mandatory for explicitness.
/// A space may also be used instead of 'T' for better readability.
/// </summary>
public class InstantParser : IArgumentParser<Instant>
{
    public Task<ArgsParseResult<Instant>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        if (args.Count >= 2)
        {
            // try with 2 arguments first, in case it contains a space instead of 'T'
            string str = $"{args[0]}T{args[1]}";
            ParseResult<Instant> resultFromTwoArgs = InstantPattern.ExtendedIso.Parse(str);
            if (resultFromTwoArgs.Success)
            {
                return Task.FromResult(ArgsParseResult<Instant>.Success(
                    resultFromTwoArgs.Value, args.Skip(2).ToImmutableList()));
            }
        }
        ParseResult<Instant> resultFromOneArgs = InstantPattern.ExtendedIso.Parse(args[0]);
        return Task.FromResult(resultFromOneArgs.Success
            ? ArgsParseResult<Instant>.Success(resultFromOneArgs.Value, args.Skip(1).ToImmutableList())
            : ArgsParseResult<Instant>.Failure($"did not recognize '{args[0]}' as a UTC-instant"));
    }
}
