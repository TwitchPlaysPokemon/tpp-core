using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser capable of parsing numbers.
    /// </summary>
    public class IntParser : BaseArgumentParser<int>
    {
        private readonly int _minValue;
        private readonly int _maxValue;

        public IntParser(int minValue = 0, int maxValue = int.MaxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override Task<ArgsParseResult<int>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            string str = args[0];
            try
            {
                int number = int.Parse(str);
                if (number < _minValue)
                {
                    return Task.FromResult(ArgsParseResult<int>.Failure(
                        $"'{str}' cannot be below {_minValue}", ErrorRelevanceConfidence.Likely));
                }
                if (number > _maxValue)
                {
                    return Task.FromResult(ArgsParseResult<int>.Failure(
                        $"'{str}' cannot be above {_maxValue}", ErrorRelevanceConfidence.Likely));
                }
                var result = ArgsParseResult<int>.Success(number, args.Skip(1).ToImmutableList());
                return Task.FromResult(result);
            }
            catch (FormatException)
            {
                return Task.FromResult(ArgsParseResult<int>.Failure($"did not recognize '{str}' as a number"));
            }
            catch (OverflowException)
            {
                return Task.FromResult(ArgsParseResult<int>.Failure(
                    $"'{str}' is out of range", ErrorRelevanceConfidence.Likely));
            }
        }
    }
}
