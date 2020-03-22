using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Core.ArgsParsing.Types;

namespace Core.ArgsParsing.TypeParsers
{
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
            if (fromInd + 1 == values.Count)
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
            IReadOnlyCollection<string> args,
            Type[] genericTypes)
        {
            var argList = args.Take(genericTypes.Length).ToList();
            foreach (var argsPermutation in Permutations(argList))
            {
                var parseResult = await _argsParser.ParseRaw(argsPermutation.ToImmutableList(), genericTypes);
                if (!parseResult.IsSuccess)
                {
                    continue;
                }
                var items = parseResult.Result;
                var type = items.Count switch
                {
                    2 => typeof(AnyOrder<,>),
                    3 => typeof(AnyOrder<,,>),
                    4 => typeof(AnyOrder<,,,>),
                    var num => throw new InvalidOperationException(
                        $"An implementation of {typeof(AnyOrder)} for {num} generic arguments " +
                        "needs to be implemented and wired up where this exception is thrown. " +
                        "But do you _really_ want this many arguments in any order?")
                };
                var constructor = type.MakeGenericType(genericTypes).GetConstructor(genericTypes);
                if (constructor == null)
                {
                    throw new InvalidOperationException($"{type} needs a constructor with {items.Count} parameters.");
                }
                return ArgsParseResult<AnyOrder>.Success((AnyOrder) constructor.Invoke(items.ToArray()),
                    parseResult.RemainingArgs);
            }
            return ArgsParseResult<AnyOrder>.Failure();
        }
    }
}
