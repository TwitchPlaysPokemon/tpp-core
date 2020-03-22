using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.ArgsParsing.Types;

namespace Core.ArgsParsing.TypeParsers
{
    public class OptionalParser : BaseArgumentParser<Optional>
    {
        private readonly ArgsParser _argsParser;

        public OptionalParser(ArgsParser argsParser)
        {
            _argsParser = argsParser;
        }

        public override async Task<ArgsParseResult<Optional>> Parse(IReadOnlyCollection<string> args,
            Type[] genericTypes)
        {
            if (genericTypes.Length != 1)
            {
                throw new ArgumentException($"Only expected 1 generic argument for {typeof(Optional)}, " +
                                            $"but got {genericTypes.Length}");
            }
            var type = typeof(Optional<>).MakeGenericType(genericTypes[0]);
            var constructor = type.GetConstructor(new[] {typeof(bool), genericTypes[0]});
            if (constructor == null)
            {
                throw new InvalidOperationException($"{type} needs a constructor (bool present, T value).");
            }

            if (!args.Any())
            {
                var optional = (Optional) constructor.Invoke(new object?[] {false, null});
                return ArgsParseResult<Optional>.Success(optional, args);
            }
            var parseResult = await _argsParser.ParseRaw(args, genericTypes);
            if (parseResult.IsSuccess)
            {
                var optional = (Optional) constructor.Invoke(new object?[] {true, parseResult.Result[0]});
                return ArgsParseResult<Optional>.Success(optional, parseResult.RemainingArgs);
            }
            else
            {
                var optional = (Optional) constructor.Invoke(new object?[] {false, null});
                return ArgsParseResult<Optional>.Success(optional, args);
            }
        }
    }
}
