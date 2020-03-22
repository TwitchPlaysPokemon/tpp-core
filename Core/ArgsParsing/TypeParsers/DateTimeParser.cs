using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Core.ArgsParsing.TypeParsers
{
    public class DateTimeParser : BaseArgumentParser<DateTime>
    {
        private static bool TryParse(string input, out DateTime dateTime)
        {
            return DateTime.TryParseExact(input, DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern,
                DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out dateTime);
        }

        public override Task<ArgsParseResult<DateTime>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes)
        {
            if (args.Count >= 2)
            {
                // try with 2 arguments first, in case it contains a space instead of 'T'
                string str = $"{args.First()} {args.Skip(1).First()}";
                if (TryParse(str, out var dateTimeFromTwoArgs))
                {
                    return Task.FromResult(ArgsParseResult<DateTime>.Success(
                        dateTimeFromTwoArgs, args.Skip(2).ToImmutableList()));
                }
            }
            return Task.FromResult(TryParse(args.First().Replace("T", " "), out var dateTime)
                ? ArgsParseResult<DateTime>.Success(dateTime, args.Skip(1).ToImmutableList())
                : ArgsParseResult<DateTime>.Failure());
        }
    }
}
