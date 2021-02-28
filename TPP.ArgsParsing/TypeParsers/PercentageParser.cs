using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.TypeParsers
{
    /// <summary>
    /// Parses floating point numbers suffixed with '%' into <see cref="Percentage"/> instances, e.g. '12.34%'.
    /// </summary>
    public class PercentageParser : BaseArgumentParser<Percentage>
    {
        public override Task<ArgsParseResult<Percentage>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string percentageStr = args[0];
            if (!percentageStr.EndsWith('%'))
            {
                return Task.FromResult(ArgsParseResult<Percentage>.Failure("percentages must end in '%'"));
            }
            string doubleStr = percentageStr[..^1];
            try
            {
                double percentage = double.Parse(doubleStr);
                if (percentage < 0)
                    return Task.FromResult(ArgsParseResult<Percentage>.Failure("percentage cannot be negative",
                        ErrorRelevanceConfidence.Likely));
                return Task.FromResult(ArgsParseResult<Percentage>.Success(new Percentage { AsPercent = percentage },
                    args.Skip(1).ToImmutableList()));
            }
            catch (FormatException)
            {
                return Task.FromResult(ArgsParseResult<Percentage>.Failure(
                    $"did not recognize '{doubleStr}' as a floating point number"));
            }
            catch (OverflowException)
            {
                return Task.FromResult(ArgsParseResult<Percentage>.Failure(
                    $"'{doubleStr}' is out of range", ErrorRelevanceConfidence.Likely));
            }
        }
    }
}
