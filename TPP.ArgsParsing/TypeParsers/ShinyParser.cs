using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Common;

namespace TPP.ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser that finds a badge form by name.
    /// </summary>
    public class ShinyParser : BaseArgumentParser<bool>
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
        public override Task<ArgsParseResult<bool>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string s = args[0];
            ArgsParseResult<bool> result;
            if (shinyWords.Contains(s))
            {
                result = ArgsParseResult<bool>.Success(true, args.Skip(1).ToImmutableList());
            }
            else if (plainWords.Contains(s))
            {
                result = ArgsParseResult<bool>.Success(false, args.Skip(1).ToImmutableList());
            }
            else
            {
                result = ArgsParseResult<bool>.Failure("The argument couldn't be understood as shiny or not", ErrorRelevanceConfidence.Unlikely);
            }              
            return Task.FromResult(result);
        }
    }
}
