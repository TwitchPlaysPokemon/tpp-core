using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser that parses values using the generic type argument of <see cref="Optional{T}"/>,
/// but returns an empty instance of <see cref="Optional{T}"/> if parsing failed, instead of failing itself.
/// If parsing succeeded, the result is also wrapped inside a <see cref="Optional{T}"/>.
/// </summary>
public class OptionalParser : IArgumentParser<Optional>
{
    private readonly ArgsParser _argsParser;

    public OptionalParser(ArgsParser argsParser)
    {
        _argsParser = argsParser;
    }

    public async Task<ArgsParseResult<Optional>> Parse(
        IImmutableList<string> args,
        Type[] genericTypes)
    {
        if (genericTypes.Length != 1)
        {
            throw new ArgumentException($"Only expected 1 generic argument for {typeof(Optional)}, " +
                                        $"but got {genericTypes.Length}");
        }
        Type type = typeof(Optional<>).MakeGenericType(genericTypes[0]);
        ConstructorInfo? constructor = type.GetConstructor(new[] { typeof(bool), genericTypes[0] });
        if (constructor == null)
        {
            throw new InvalidOperationException($"{type} needs a constructor (bool present, T value).");
        }

        if (!args.Any())
        {
            var optional = (Optional)constructor.Invoke(new object?[] { false, null });
            return ArgsParseResult<Optional>.Success(optional, args);
        }
        ArgsParseResult<List<object>> parseResult = await _argsParser.ParseRaw(args, genericTypes);
        if (parseResult.SuccessResult != null)
        {
            Success<List<object>> success = parseResult.SuccessResult.Value;
            var optional = (Optional)constructor.Invoke(new[] { true, success.Result[0] });
            return ArgsParseResult<Optional>.Success(parseResult.Failures, optional, success.RemainingArgs);
        }
        else
        {
            var optional = (Optional)constructor.Invoke(new object?[] { false, null });
            return ArgsParseResult<Optional>.Success(parseResult.Failures, optional, args);
        }
    }
}
