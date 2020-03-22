using System.Collections.Generic;

namespace Core.ArgsParsing
{
    public class ArgsParseResult<T>
    {
        public bool IsSuccess { get; }
        public T Result { get; }
        public IReadOnlyCollection<string> RemainingArgs { get; }

        private ArgsParseResult(bool isSuccess, T result, IReadOnlyCollection<string> remainingArgs)
        {
            IsSuccess = isSuccess;
            Result = result;
            RemainingArgs = remainingArgs;
        }

        public static ArgsParseResult<T> Success(T result, IReadOnlyCollection<string> remainingArgs)
        {
            return new ArgsParseResult<T>(true, result, remainingArgs);
        }

        // public static ArgsParseResult<T> Success(T result)
        // {
        //     return new ArgsParseResult<T>(true, result, new string[] { });
        // }

        public static ArgsParseResult<T> Failure()
        {
            return new ArgsParseResult<T>(false, default!, default!);
        }

        public bool TryUnpack(out T result)
        {
            result = Result;
            return IsSuccess;
        }
    }
}
