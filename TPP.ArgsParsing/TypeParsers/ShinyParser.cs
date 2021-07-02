using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Common;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser that determines if something is indicated to be shiny or not.
    /// </summary>
    public class ShinyParser : BaseArgumentParser<Shiny>
    {
        string[] shinyWords =
        {
            "shiny",
            "shiny:true"
        };
        string[] plainWords =
        {
            "plain",
            "regular",
            "shiny:false"
        };
        public override Task<ArgsParseResult<Shiny>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string s = args[0];
            ArgsParseResult<Shiny> result;
            if (shinyWords.Contains(s))
            {
                result = ArgsParseResult<Shiny>.Success(new Shiny { Value = true }, args.Skip(1).ToImmutableList());
            }
            else if (plainWords.Contains(s))
            {
                result = ArgsParseResult<Shiny>.Success(new Shiny { Value = false }, args.Skip(1).ToImmutableList());
            }
            else
            {
                result = ArgsParseResult<Shiny>.Failure("The argument couldn't be understood as shiny or not", ErrorRelevanceConfidence.Unlikely);
            }              
            return Task.FromResult(result);
        }
    }
}
