using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser capable of parsing date times in strict ISO-8601 UTC format, for example <c>2014-02-12T15:30:00Z</c>.
    /// The timezone specifier 'Z' is mandatory for explicitness.
    /// A space may also be used instead of 'T' for better readability.
    /// </summary>
    public class DateTimeUtcParser : BaseArgumentParser<DateTime>
    {
        private static bool TryParse(string input, out DateTime dateTime)
        {
            return DateTime.TryParseExact(input, DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern,
                DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out dateTime);
        }

        public override Task<ArgsParseResult<DateTime>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            if (args.Count >= 2)
            {
                // try with 2 arguments first, in case it contains a space instead of 'T'
                string str = $"{args[0]} {args[1]}";
                if (TryParse(str, out DateTime dateTimeFromTwoArgs))
                {
                    return Task.FromResult(ArgsParseResult<DateTime>.Success(
                        dateTimeFromTwoArgs, args.Skip(2).ToImmutableList()));
                }
            }
            return Task.FromResult(TryParse(args[0].Replace("T", " "), out DateTime dateTime)
                ? ArgsParseResult<DateTime>.Success(dateTime, args.Skip(1).ToImmutableList())
                : ArgsParseResult<DateTime>.Failure($"did not recognize '{args[0]}' as a UTC-datetime"));
        }
    }
}
