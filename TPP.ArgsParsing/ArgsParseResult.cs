namespace TPP.ArgsParsing;

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

public readonly record struct Success<T>(T Result, IImmutableList<string> RemainingArgs);

public readonly record struct Failure(ErrorRelevanceConfidence Relevance, string Error);

/// <summary>
/// A result object that can either be successful or not,
/// and contains an instance of <typeparamref name="T"/> on success.
/// </summary>
/// <param name="Failures">The result object, if successful.</param>
/// <param name="SuccessResult">All failures that occured during parsing.
/// Note that even successful results may contain failures, which is needed for better error reporting,
/// e.g. to be able to pick the most relevant error message if parsing fails at a later, less relevant point.</param>
/// <typeparam name="T">type of the success object that will be contained on success.</typeparam>
public readonly record struct ArgsParseResult<T>(Success<T>? SuccessResult, IImmutableList<Failure> Failures)
{
    /// <summary>
    /// Create a successful parse result object.
    /// </summary>
    /// <param name="failures">The failures encountered during parsing.</param>
    /// <param name="result">The successfully parsed object.</param>
    /// <param name="remainingArgs">The remaining arguments that were not consumed.</param>
    /// <returns>An respective instance of <see cref="ArgsParseResult{T}"/></returns>
    public static ArgsParseResult<T> Success(
        IImmutableList<Failure> failures,
        T result,
        IImmutableList<string> remainingArgs)
    {
        return new ArgsParseResult<T>(new Success<T>{Result = result, RemainingArgs = remainingArgs}, failures);
    }

    /// <summary>
    /// Create a successful parse result object, without any failures.
    /// </summary>
    /// <param name="result">The successfully parsed object.</param>
    /// <param name="remainingArgs">The remaining arguments that were not consumed.</param>
    /// <returns>An respective instance of <see cref="ArgsParseResult{T}"/></returns>
    public static ArgsParseResult<T> Success(
        T result,
        IImmutableList<string> remainingArgs)
    {
        return new ArgsParseResult<T>(new Success<T>(result, remainingArgs), ImmutableList<Failure>.Empty);
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
        return new ArgsParseResult<T>(null, ImmutableList.Create(new Failure(relevance, message)));
    }

    /// <summary>
    /// Create an unsuccessful parse result object from existing failures.
    /// </summary>
    /// <param name="failures">The failures that are part of this parse result.</param>
    /// <returns>An respective instance of <see cref="ArgsParseResult{T}"/></returns>
    public static ArgsParseResult<T> Failure(IImmutableList<Failure> failures)
    {
        return new ArgsParseResult<T>(null, failures);
    }
}
