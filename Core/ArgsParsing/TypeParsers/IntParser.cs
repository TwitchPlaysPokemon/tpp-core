using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Core.ArgsParsing.TypeParsers
{
    public class IntParser : BaseArgumentParser<int>
    {
        public override Task<ArgsParseResult<int>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes)
        {
            try
            {
                int number = int.Parse(args.First());
                var result = ArgsParseResult<int>.Success(number, args.Skip(1).ToImmutableList());
                return Task.FromResult(result);
            }
            catch (FormatException)
            {
                return Task.FromResult(ArgsParseResult<int>.Failure());
            }
            catch (OverflowException)
            {
                return Task.FromResult(ArgsParseResult<int>.Failure());
            }
        }
    }
}
