using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser capable of recognizing time spans in the format
/// <c>&lt;weeks&gt;w&lt;days&gt;d&lt;hours&gt;h&lt;minutes&gt;m&lt;seconds&gt;s</c>
/// and turning them into instances of <see cref="TimeSpan"/>.
/// </summary>
public class TimeSpanParser : IArgumentParser<TimeSpan>
{
    private static readonly Regex Regex = new Regex(
        @"^
            (?:(?<weeks>[0-9]+)w)?
            (?:(?<days>[0-9]+)d)?
            (?:(?<hours>[0-9]+)h)?
            (?:(?<minutes>[0-9]+)m)?
            (?:(?<seconds>[0-9]+)s)?
            $",
        RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

    public Task<ArgsParseResult<TimeSpan>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        string str = args[0];
        Match match = Regex.Match(str);
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
                return Task.FromResult(ArgsParseResult<TimeSpan>.Success(
                    timeSpan, args.Skip(1).ToImmutableList()));
            }
            catch (FormatException)
            {
                return Task.FromResult(ArgsParseResult<TimeSpan>.Failure(
                    $"did not recognize '{str}' as a duration"));
            }
            catch (OverflowException)
            {
                return Task.FromResult(ArgsParseResult<TimeSpan>.Failure(
                    $"the duration described by '{str}' is out of range", ErrorRelevanceConfidence.Likely));
            }
        }
        else
        {
            return Task.FromResult(ArgsParseResult<TimeSpan>.Failure(
                $"did not recognize '{str}' as a duration in the form " +
                "'<weeks>w<days>d<hours>h<minutes>m<seconds>s' (zeroes may be omitted, e.g. '1d12h')"));
        }
    }
}
