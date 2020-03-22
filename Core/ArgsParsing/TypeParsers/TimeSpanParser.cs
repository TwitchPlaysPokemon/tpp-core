using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Core.ArgsParsing.TypeParsers
{
    public class TimeSpanParser : BaseArgumentParser<TimeSpan>
    {
        private static readonly Regex Regex = new Regex(
            @"^
            (?:(?<weeks>\d+)w)?
            (?:(?<days>\d+)d)?
            (?:(?<hours>\d+)h)?
            (?:(?<minutes>\d+)m)?
            (?:(?<seconds>\d+)s)?
            $",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        public override Task<ArgsParseResult<TimeSpan>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes)
        {
            var match = Regex.Match(args.First());
            if (match.Success)
            {
                try
                {
                    string weeks = match.Groups["weeks"].Value;
                    string days = match.Groups["days"].Value;
                    string hours = match.Groups["hours"].Value;
                    string minutes = match.Groups["minutes"].Value;
                    string seconds = match.Groups["seconds"].Value;
                    var timeSpan = new TimeSpan(
                        days: (weeks.Length > 0 ? int.Parse(weeks) : 0) * 7
                              + (days.Length > 0 ? int.Parse(days) : 0),
                        hours: hours.Length > 0 ? int.Parse(hours) : 0,
                        minutes: minutes.Length > 0 ? int.Parse(minutes) : 0,
                        seconds: seconds.Length > 0 ? int.Parse(seconds) : 0
                    );
                    return Task.FromResult(ArgsParseResult<TimeSpan>.Success(timeSpan, args.Skip(1).ToImmutableList()));
                }
                catch (Exception e) when (e is ArithmeticException || e is ArgumentOutOfRangeException)
                {
                    return Task.FromResult(ArgsParseResult<TimeSpan>.Failure());
                }
            }
            else
            {
                return Task.FromResult(ArgsParseResult<TimeSpan>.Failure());
            }
        }
    }
}
