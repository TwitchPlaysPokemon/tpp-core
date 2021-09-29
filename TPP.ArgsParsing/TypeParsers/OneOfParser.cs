using System.Diagnostics;
using System.Reflection;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser that parses values using the generic type arguments of any of the derived classes
/// of <see cref="OneOf"/>, parsing only ever exactly one of the generic arguments.
/// For example parsing <c>OneOf&lt;int, string&gt;</c> will succeed for both
/// <c>"123"</c> and <c>"foo"</c>, and fill in the first or second item respectively.
/// In case of multiple possible matches, the first match gets returned.
/// </summary>
public class OneOfParser : IArgumentParser<OneOf>
{
    private readonly ArgsParser _argsParser;

    public OneOfParser(ArgsParser argsParser)
    {
        _argsParser = argsParser;
    }

    public async Task<ArgsParseResult<OneOf>> Parse(
        IImmutableList<string> args,
        Type[] genericTypes)
    {
        var failures = new List<Failure>();
        for (int i = 0; i < genericTypes.Length; i++)
        {
            Type nestedType = genericTypes[i];
            ArgsParseResult<List<object>> parseResult = await _argsParser.ParseRaw(args, new[] { nestedType });
            if (parseResult.SuccessResult == null)
            {
                Debug.Assert(parseResult.Failures.Any());
                failures.AddRange(parseResult.Failures);
                continue;
            }
            object result = parseResult.SuccessResult.Value.Result.First();
            Type type = genericTypes.Length switch
            {
                2 => typeof(OneOf<,>),
                3 => typeof(OneOf<,,>),
                4 => typeof(OneOf<,,,>),
                var num => throw new InvalidOperationException(
                    $"An implementation of {typeof(OneOf)} for {num} generic arguments " +
                    "needs to be implemented and wired up where this exception is thrown. " +
                    "But do you _really_ want \"one of\" this many arguments?")
            };
            ConstructorInfo? oneOfConstructor = type
                .MakeGenericType(genericTypes)
                .GetConstructor(genericTypes.Select(t => typeof(Optional<>).MakeGenericType(t)).ToArray());
            if (oneOfConstructor == null)
            {
                throw new InvalidOperationException(
                    $"{type} needs a constructor with {genericTypes.Length} parameters.");
            }
            object[] invokeArgs = new object[genericTypes.Length];
            for (int j = 0; j < invokeArgs.Length; j++)
            {
                // create Optional<> instances. their constructor takes (bool success, object value)
                Type genericType = genericTypes[j];
                ConstructorInfo? optionalConstructor = typeof(Optional<>)
                    .MakeGenericType(genericType)
                    .GetConstructor(new[] { typeof(bool), genericType });
                if (optionalConstructor == null)
                {
                    throw new InvalidOperationException($"{typeof(Optional<>)} needs a constructor (bool, object)");
                }
                invokeArgs[j] = i == j // for the successful one, fill the optional with the result
                    ? optionalConstructor.Invoke(new[] { true, result })
                    : optionalConstructor.Invoke(new object[] { false, null! });
            }
            return ArgsParseResult<OneOf>.Success(
                parseResult.Failures,
                (OneOf)oneOfConstructor.Invoke(invokeArgs),
                parseResult.SuccessResult.Value.RemainingArgs);
        }
        Debug.Assert(failures.Any());
        return ArgsParseResult<OneOf>.Failure(failures.ToImmutableList());
    }
}
