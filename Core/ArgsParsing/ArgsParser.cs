using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.ArgsParsing
{
    public class ArgsParser
    {

        private readonly Dictionary<Type, IArgumentParser> _parsers = new Dictionary<Type, IArgumentParser>();

        public void AddArgumentParser<T>(IArgumentParser<T> argumentParser)
        {
            _parsers.Add(typeof(T), argumentParser);
        }

        static ArgsParser()
        {
            CultureFix.UseInvariantCulture();
        }

        public async Task<ArgsParseResult<List<object>>> ParseRaw(
            IReadOnlyCollection<string> args,
            IEnumerable<Type> types)
        {
            var remainingArgs = args;
            var result = new List<object>();
            foreach (var type in types)
            {
                var queryType = type.IsGenericType ? type.BaseType! : type;
                if (!_parsers.TryGetValue(queryType, out var parser))
                {
                    throw new ArgumentException($"No parser found for type {type}");
                }
                var genericTypes = type.IsGenericType ? type.GenericTypeArguments : new Type[] { };
                var parseResult = await parser.Parse(remainingArgs, genericTypes);
                if (!parseResult.IsSuccess)
                {
                    return ArgsParseResult<List<object>>.Failure();
                }
                remainingArgs = parseResult.RemainingArgs;
                result.Add(parseResult.Result);
            }
            return ArgsParseResult<List<object>>.Success(result, remainingArgs);
        }

        public async Task<ArgsParseResult<T1>> TryParse<T1>(IReadOnlyCollection<string> args)
        {
            IEnumerable<Type> types = new[] {typeof(T1)};
            var parseResult = await ParseRaw(args, types);
            if (!parseResult.IsSuccess || parseResult.RemainingArgs.Any())
            {
                return ArgsParseResult<T1>.Failure();
            }
            var result = parseResult.Result;
            return ArgsParseResult<T1>.Success((T1) result[0], parseResult.RemainingArgs);
        }

        public async Task<ArgsParseResult<(T1, T2)>> TryParse<T1, T2>(IReadOnlyCollection<string> args)
        {
            IEnumerable<Type> types = new[] {typeof(T1), typeof(T2)};
            var parseResult = await ParseRaw(args, types);
            if (!parseResult.IsSuccess || parseResult.RemainingArgs.Any())
            {
                return ArgsParseResult<(T1, T2)>.Failure();
            }
            var result = parseResult.Result;
            var tuple = ((T1) result[0], (T2) result[1]);
            return ArgsParseResult<(T1, T2)>.Success(tuple, parseResult.RemainingArgs);
        }

        public async Task<T1> Parse<T1>(IReadOnlyCollection<string> args)
        {
            var parseResult = await TryParse<T1>(args);
            if (parseResult.TryUnpack(out var result))
            {
                return result;
            }
            throw new ArgsParseFailure("TODO felk");
        }

        public async Task<(T1, T2)> Parse<T1, T2>(IReadOnlyCollection<string> args)
        {
            var parseResult = await TryParse<T1, T2>(args);
            if (parseResult.TryUnpack(out var result))
            {
                return result;
            }
            throw new ArgsParseFailure("TODO felk");
        }
    }
}
