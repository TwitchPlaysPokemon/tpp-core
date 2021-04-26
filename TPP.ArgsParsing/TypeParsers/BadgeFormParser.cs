using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Persistence.Models;

namespace TPP.ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser that finds a badge form by name.
    /// </summary>
    public class BadgeFormParser : BaseArgumentParser<Badge.BadgeForm>
    {
        public override Task<ArgsParseResult<Badge.BadgeForm>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string form = args[0];
            ArgsParseResult<Badge.BadgeForm> result;
            try
            {
                Badge.BadgeForm parsedForm = (Badge.BadgeForm)Enum.Parse(typeof(Badge.BadgeForm), form, ignoreCase: true);
                if (parsedForm == Badge.BadgeForm.Shiny)
                {
                    switch (args[1].ToLower())
                    {
                        case "shadow":
                            result = ArgsParseResult<Badge.BadgeForm>.Success(Badge.BadgeForm.ShinyShadow, args.Skip(2).ToImmutableList());
                            break;
                        case "mega":
                            result = ArgsParseResult<Badge.BadgeForm>.Success(Badge.BadgeForm.ShinyMega, args.Skip(2).ToImmutableList());
                            break;
                        case "alolan":
                            result = ArgsParseResult<Badge.BadgeForm>.Success(Badge.BadgeForm.ShinyAlolan, args.Skip(2).ToImmutableList());
                            break;
                        case "galarian":
                            result = ArgsParseResult<Badge.BadgeForm>.Success(Badge.BadgeForm.ShinyGalarian, args.Skip(2).ToImmutableList());
                            break;
                        default:
                            result = ArgsParseResult<Badge.BadgeForm>.Success(parsedForm, args.Skip(1).ToImmutableList());
                            break;
                    }
                }
                else
                {
                    result = ArgsParseResult<Badge.BadgeForm>.Success(parsedForm, args.Skip(1).ToImmutableList());
                }
            }
            catch (ArgumentException)
            {
                result = ArgsParseResult<Badge.BadgeForm>.Failure($"Did not find a role named '{form}'");
            }
            return Task.FromResult(result);
        }
    }
}
