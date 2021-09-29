using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser that parses values using the generic type argument of <see cref="ManyOf{T}"/>,
/// which essentially represent a list of homogeneously typed values.
/// Parsing never fails, but the resulting list may be empty.
/// <br/>
/// The parser has a few restrictions:
/// <ul>
///   <li>
///   It is assumed that each list entry consumes at least one argument, so it will not work with e.g. Optional.
///   </li>
///   <li>
///   It just greedily parses as many arguments as possible. When used in a way where parsing may be ambiguous to
///   any subsequent types, the parser will consume arguments that were meant for that subsequent type.
///   Since there is no backtracking for successes, overall parsing will then fail because parsing the subsequent
///   type runs out of arguments.
///   Avoid these situations by not using <see cref="ManyOf{T}"/> followed by ambiguously parsed types.
///   </li>
/// </ul>
/// </summary>
public class ManyOfParser : IArgumentParser<ManyOf>
{
    private readonly ArgsParser _argsParser;

    public ManyOfParser(ArgsParser argsParser)
    {
        _argsParser = argsParser;
    }

    public async Task<ArgsParseResult<ManyOf>> Parse(
        IImmutableList<string> args,
        Type[] genericTypes)
    {
        if (genericTypes.Length != 1)
            throw new ArgumentException("list parser must receive exactly 1 generic type", nameof(genericTypes));
        Type listContentType = genericTypes[0];

        Type manyOfType = typeof(ManyOf<>).MakeGenericType(listContentType);
        ConstructorInfo? constructor = manyOfType.GetConstructor(new[] { typeof(IEnumerable<object>) });
        if (constructor == null)
            throw new InvalidOperationException(
                $"{manyOfType} needs a constructor (IEnumerable<object> values).");

        List<Failure> failures = new();
        for (int numArgs = args.Count; numArgs > 0; numArgs--)
        {
            ArgsParseResult<List<object>> result = await _argsParser
                .ParseRaw(args, Enumerable.Repeat(listContentType, numArgs));
            failures.AddRange(result.Failures);
            if (result.SuccessResult != null)
            {
                List<object> contents = result.SuccessResult.Value.Result;
                return ArgsParseResult<ManyOf>.Success(
                    failures.ToImmutableList(),
                    (ManyOf)constructor.Invoke(new object[] { contents }),
                    result.SuccessResult.Value.RemainingArgs
                );
            }
        }
        return ArgsParseResult<ManyOf>.Success(
            failures.ToImmutableList(),
            (ManyOf)constructor.Invoke(new object?[] { ImmutableList.Create<object>() }),
            args
        );
    }
}
