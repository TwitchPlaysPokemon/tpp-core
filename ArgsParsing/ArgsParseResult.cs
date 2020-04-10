using System;
using System.Collections.Immutable;

namespace ArgsParsing
{
    public enum ErrorRelevanceConfidence
    {
        /// <summary>
        /// Indicates that this error is unlikely to be helpful in determining why overall parsing might have failed.
        /// Other errors should preferably be reported instead.
        /// </summary>
        Unlikely,

        /// <summary>
        /// Indicates that this error does not know or does not want to express its meaningfulness.
        /// </summary>
        Default,

        /// <summary>
        /// Indicates that this error is likely to accurately describe why parsing failed overall,
        /// and should preferably be reported over other, less helpful errors.
        /// </summary>
        Likely,
    }

    public readonly struct Success<T>
    {
        public T Result { get; }
        public IImmutableList<string> RemainingArgs { get; }

        public Success(T result, IImmutableList<string> remainingArgs)
        {
            Result = result;
            RemainingArgs = remainingArgs;
        }
    }

    public readonly struct Failure
    {
        public ErrorRelevanceConfidence Relevance { get; }
        public string Error { get; }

        public Failure(ErrorRelevanceConfidence relevance, string error)
        {
            Relevance = relevance;
            Error = error;
        }
    }

    /// <summary>
    /// A result object that can either be successful or not,
    /// and contains an instance of <typeparamref name="T"/> on success.
    /// </summary>
    /// <typeparam name="T">type of the success object that will be contained on success.</typeparam>
    public readonly struct ArgsParseResult<T>
    {
        /// <summary>
        /// Whether this success object resulted from a successful parse operation, and therefore contains a result.
        /// </summary>
        public bool IsSuccess { get; }

        private readonly Success<T> _successResult;
        /// <summary>
        /// The result object, if successful.
        /// </summary>
        public Success<T> SuccessResult
        {
            get
            {
                if (!IsSuccess)
                    throw new InvalidOperationException(
                        "Cannot access the success object of an unsuccessful parse result.");
                return _successResult;
            }
        }
        /// <summary>
        /// A failure that occured during parsing.
        /// Note that even successful results may contain failures, which is needed for better error reporting,
        /// e.g. to be able to pick the most relevant error message if parsing fails at a later, less relevant point.
        /// </summary>
        public Failure? FailureResult { get; }

        private ArgsParseResult(
            bool isSuccess,
            Success<T> successResult,
            Failure? failureResult)
        {
            IsSuccess = isSuccess;
            _successResult = successResult;
            FailureResult = failureResult;
        }

        /// <summary>
        /// Create a successful parse result object.
        /// </summary>
        /// <param name="nestedFailure">The most significant failure encountered during parsing.</param>
        /// <param name="result">The successfully parsed object.</param>
        /// <param name="remainingArgs">The remaining arguments that were not consumed.</param>
        /// <returns>An respective instance of <see cref="ArgsParseResult{T}"/></returns>
        public static ArgsParseResult<T> Success(
            Failure? nestedFailure,
            T result,
            IImmutableList<string> remainingArgs)
        {
            return new ArgsParseResult<T>(true, new Success<T>(result, remainingArgs), nestedFailure);
        }

        /// <summary>
        /// Create a successful parse result object, without any nested failure.
        /// </summary>
        /// <param name="result">The successfully parsed object.</param>
        /// <param name="remainingArgs">The remaining arguments that were not consumed.</param>
        /// <returns>An respective instance of <see cref="ArgsParseResult{T}"/></returns>
        public static ArgsParseResult<T> Success(
            T result,
            IImmutableList<string> remainingArgs)
        {
            return new ArgsParseResult<T>(true, new Success<T>(result, remainingArgs), null);
        }

        /// <summary>
        /// Create an unsuccessful parse result object, without any nested failure.
        /// </summary>
        /// <param name="message">A message describing the error.</param>
        /// <param name="relevance">(optional) How likely it is that this error is relevant to the overall parsing,
        /// or in other words how useful it will probably be if reported to the user.</param>
        /// <returns>An respective instance of <see cref="ArgsParseResult{T}"/></returns>
        public static ArgsParseResult<T> Failure(
            string message,
            ErrorRelevanceConfidence relevance = ErrorRelevanceConfidence.Default)
        {
            return new ArgsParseResult<T>(false, default!, new Failure(relevance, message));
        }

        /// <summary>
        /// Create an unsuccessful parse result object from an existing failure.
        /// </summary>
        /// <param name="failure">The existing failure to re-use the failure from.</param>
        /// <returns>An respective instance of <see cref="ArgsParseResult{T}"/></returns>
        public static ArgsParseResult<T> Failure(Failure? failure)
        {
            return new ArgsParseResult<T>(false, default!, failure);
        }

        /// <summary>
        /// Accesses the parsing result, if <see cref="IsSuccess"/> is true.
        /// </summary>
        /// <param name="result">The contained parsing result.</param>
        /// <param name="remainingArgs">The remaining, unconsumed arguments.</param>
        /// <returns>Whether the result is successful and the out variable therefore set.</returns>
        public bool TryUnwrap(out T result, out IImmutableList<string> remainingArgs)
        {
            if (IsSuccess)
            {
                result = SuccessResult.Result;
                remainingArgs = SuccessResult.RemainingArgs;
                return true;
            }
            else
            {
                result = default!;
                remainingArgs = default!;
                return false;
            }
        }

        /// <summary>
        /// Overload of <see cref="TryUnwrap(out T,out System.Collections.Immutable.IImmutableList{string})"/>
        /// that ignores any remaining arguments.
        /// </summary>
        /// <param name="result">The contained parsing result.</param>
        /// <returns>Whether the result is successful and the out variable therefore set.</returns>
        public bool TryUnwrap(out T result)
        {
            return TryUnwrap(out result, out _);
        }
    }
}
