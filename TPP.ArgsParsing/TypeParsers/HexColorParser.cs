using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TPP.Common;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// Parser capable of parsing colors represented as a 6-digit hexadecimal string optionally prefixed with '#',
/// for example <c>#ff0000</c> for pure red, which will result in a <see cref="HexColor"/> of <c>#FF0000</c>.
/// </summary>
public class HexColorParser : IArgumentParser<HexColor>
{
    private static readonly Regex Regex = new(@"^#?[0-9a-f]{6}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<ArgsParseResult<HexColor>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        Match colorMatch = Regex.Match(args[0]);
        if (colorMatch.Success)
        {
            string colorUpper = colorMatch.Value.ToUpper();
            HexColor color = colorUpper.StartsWith('#')
                ? HexColor.FromWithHash(colorUpper)
                : HexColor.FromWithoutHash(colorUpper);
            return Task.FromResult(ArgsParseResult<HexColor>.Success(color, args.Skip(1).ToImmutableList()));
        }
        else
        {
            return Task.FromResult(args[0].StartsWith("#")
                ? ArgsParseResult<HexColor>.Failure(
                    $"'{args[0]}' must be a 6-character hex code consisting of 0-9 and A-F, " +
                    "for example '#FF0000' for pure red.", ErrorRelevanceConfidence.Likely)
                : ArgsParseResult<HexColor>.Failure($"'{args[0]}' is not a valid hex color"));
        }
    }
}
