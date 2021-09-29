using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser capable of parsing numbers.
/// </summary>
public abstract class IntParser<T> : IArgumentParser<T> where T : ImplicitNumber, new()
{
    private readonly int _minValue;
    private readonly int _maxValue;

    protected IntParser(int minValue, int maxValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;
    }

    public Task<ArgsParseResult<T>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        string str = args[0];
        try
        {
            int number = int.Parse(str);
            if (number < _minValue)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure(
                    $"'{str}' cannot be below {_minValue}", ErrorRelevanceConfidence.Likely));
            }
            if (number > _maxValue)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure(
                    $"'{str}' cannot be above {_maxValue}", ErrorRelevanceConfidence.Likely));
            }
            ArgsParseResult<T> result =
                ArgsParseResult<T>.Success(new T { Number = number }, args.Skip(1).ToImmutableList());
            return Task.FromResult(result);
        }
        catch (FormatException)
        {
            return Task.FromResult(ArgsParseResult<T>.Failure($"did not recognize '{str}' as a number"));
        }
        catch (OverflowException)
        {
            return Task.FromResult(ArgsParseResult<T>.Failure(
                $"'{str}' is out of range", ErrorRelevanceConfidence.Likely));
        }
    }
}

public class SignedIntParser : IntParser<SignedInt>
{
    public SignedIntParser() : base(int.MinValue, int.MaxValue)
    {
    }
}

public class NonNegativeIntParser : IntParser<NonNegativeInt>
{
    public NonNegativeIntParser() : base(0, int.MaxValue)
    {
    }
}

public class PositiveIntParser : IntParser<PositiveInt>
{
    public PositiveIntParser() : base(1, int.MaxValue)
    {
    }
}
