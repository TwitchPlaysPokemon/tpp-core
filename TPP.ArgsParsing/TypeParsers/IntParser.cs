using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser capable of parsing numbers.
/// </summary>
public abstract class IntParser<T>(int minValue, int maxValue) : IArgumentParser<T>
    where T : ImplicitNumber, new()
{
    public Task<ArgsParseResult<T>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        string str = args[0];
        try
        {
            int number = int.Parse(str);
            if (number < minValue)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure(
                    $"'{str}' cannot be below {minValue}", ErrorRelevanceConfidence.Likely));
            }
            if (number > maxValue)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure(
                    $"'{str}' cannot be above {maxValue}", ErrorRelevanceConfidence.Likely));
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

public class SignedIntParser() : IntParser<SignedInt>(int.MinValue, int.MaxValue);

public class NonNegativeIntParser() : IntParser<NonNegativeInt>(0, int.MaxValue);

public class PositiveIntParser() : IntParser<PositiveInt>(1, int.MaxValue);
