using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArgsParsing.Types;

namespace ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser that parses values using the generic type arguments of any of the derived classes
    /// of <see cref="AnyOrder"/>, trying out all possible permutations.
    /// For example parsing <c>AnyOrder&lt;int, string&gt;</c> will succeed for both
    /// <c>{"123", "foo"}</c> and <c>{"foo", "123"}</c>, but not for <c>{"foo", "bar"}</c>.
    /// In case of multiple possible permutations, the first matching permutation gets returned.
    /// </summary>
    public class AnyOrderParser : BaseArgumentParser<AnyOrder>
    {
        private readonly ArgsParser _argsParser;

        public AnyOrderParser(ArgsParser argsParser)
        {
            _argsParser = argsParser;
        }

        // https://stackoverflow.com/a/13022090
        private static IEnumerable<IList<T>> Permutations<T>(IList<T> values, int fromInd = 0)
        {
            if (fromInd + 1 >= values.Count)
            {
                yield return values;
            }
            else
            {
                foreach (var v in Permutations(values, fromInd + 1))
                {
                    yield return v;
                }

                for (int i = fromInd + 1; i < values.Count; i++)
                {
                    (values[fromInd], values[i]) = (values[i], values[fromInd]);
                    foreach (var v in Permutations(values, fromInd + 1))
                        yield return v;
                    (values[fromInd], values[i]) = (values[i], values[fromInd]);
                }
            }
        }

        public override async Task<ArgsParseResult<AnyOrder>> Parse(
            IImmutableList<string> args,
            Type[] genericTypes)
        {
            var argList = args.Take(genericTypes.Length).ToList();
            var failures = new List<Failure>();
            foreach (var argsPermutation in Permutations(argList))
            {
                ArgsParseResult<List<object>> parseResult = await _argsParser
                    .ParseRaw(argsPermutation.ToImmutableList(), genericTypes);
                if (parseResult.SuccessResult == null)
                {
                    Debug.Assert(parseResult.Failures.Any());
                    failures.AddRange(parseResult.Failures);
                    continue;
                }
                List<object> items = parseResult.SuccessResult.Value.Result;
                Type type = items.Count switch
                {
                    2 => typeof(AnyOrder<,>),
                    3 => typeof(AnyOrder<,,>),
                    4 => typeof(AnyOrder<,,,>),
                    var num => throw new InvalidOperationException(
                        $"An implementation of {typeof(AnyOrder)} for {num} generic arguments " +
                        "needs to be implemented and wired up where this exception is thrown. " +
                        "But do you _really_ want this many arguments in any order?")
                };
                ConstructorInfo? constructor = type.MakeGenericType(genericTypes).GetConstructor(genericTypes);
                if (constructor == null)
                {
                    throw new InvalidOperationException($"{type} needs a constructor with {items.Count} parameters.");
                }
                return ArgsParseResult<AnyOrder>.Success(
                    parseResult.Failures,
                    (AnyOrder)constructor.Invoke(items.ToArray()),
                    parseResult.SuccessResult.Value.RemainingArgs);
            }
            Debug.Assert(failures.Any());
            return ArgsParseResult<AnyOrder>.Failure(failures.ToImmutableList());
        }
    }
}
