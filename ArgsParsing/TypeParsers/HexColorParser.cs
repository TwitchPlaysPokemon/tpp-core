using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArgsParsing.Types;

namespace ArgsParsing.TypeParsers
{
    /// <summary>
    /// Parser capable of parsing colors represented as a 6-digit hexadecimal string prefixed with '#',
    /// for example <c>#ff0000</c> for pure red, which will result in a <see cref="HexColor"/> of <c>#FF0000</c>.
    /// </summary>
    public class HexColorParser : BaseArgumentParser<HexColor>
    {
        private readonly Regex _regex = new Regex(@"^#[0-9a-f]{6}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public override Task<ArgsParseResult<HexColor>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            Match colorMatch = _regex.Match(args[0]);
            if (colorMatch.Success)
            {
                var color = new HexColor(colorMatch.Value.ToUpper());
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
}
