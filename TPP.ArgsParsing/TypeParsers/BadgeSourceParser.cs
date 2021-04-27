using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Persistence.Models;

namespace TPP.ArgsParsing.TypeParsers
{
    public class BadgeSourceParser : BaseArgumentParser<Badge.BadgeSource>
    {
        public override Task<ArgsParseResult<Badge.BadgeSource>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string source = args[0];
            ArgsParseResult<Badge.BadgeSource> result;
            Badge.BadgeSource? parsedSource = null;
            try
            {
                parsedSource = (Badge.BadgeSource)Enum.Parse(typeof(Badge.BadgeSource), source, ignoreCase: true);
            }
            catch (ArgumentException)
            {
                switch (args[1].ToLower())
                {
                    case "run":
                    case "caught":
                        parsedSource = Badge.BadgeSource.RunCaught;
                        break;
                }
            }
            if (parsedSource != null)
                result = ArgsParseResult<Badge.BadgeSource>.Success(parsedSource.Value, args.Skip(1).ToImmutableList());
            else
                result = ArgsParseResult<Badge.BadgeSource>.Failure($"Did not find a source named '{args[0]}'");
            return Task.FromResult(result);
        }
    }
}
