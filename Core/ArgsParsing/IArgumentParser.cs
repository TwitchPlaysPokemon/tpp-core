using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.ArgsParsing
{
    public interface IArgumentParser
    {
        Task<ArgsParseResult<object>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes);
    }

    public interface IArgumentParser<T> : IArgumentParser
    {
        new Task<ArgsParseResult<T>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes);
    }

    public abstract class BaseArgumentParser<T> : IArgumentParser<T>
    {
        public abstract Task<ArgsParseResult<T>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes);

        async Task<ArgsParseResult<object>> IArgumentParser.Parse(IReadOnlyCollection<string> args, Type[] genericTypes)
        {
            var parseResult = await Parse(args, genericTypes);
            return parseResult.IsSuccess
                ? ArgsParseResult<object>.Success(parseResult.Result!, parseResult.RemainingArgs)
                : ArgsParseResult<object>.Failure();
        }
    }
}
