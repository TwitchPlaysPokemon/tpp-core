using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace TPP.ArgsParsing;

/// <summary>
/// Non-generic version of <see cref="IArgumentParser{T}"/>.
/// For internal use only, do not implement this interface!
/// </summary>
public interface IArgumentParser
{
    Task<ArgsParseResult<object>> Parse(IImmutableList<string> args, Type[] genericTypes);
}

/// <summary>
/// Interface describing the ability to turn a list of strings into instances of a list of given types.
/// </summary>
/// <typeparam name="T">type this parser can create instances of using the supplied string arguments.
/// If the type is supposed to be generic, e.g. <c>Foo&lt;T&gt;</c>, the generic type needs to have a non-generic
/// base class (e.g. <c>Foo&lt;T&gt; : Foo</c>) for which the parser shall be implemented.</typeparam>
public interface IArgumentParser<T> : IArgumentParser
{
    /// <summary>
    /// Parse the given string arguments into an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="args">String arguments to parse from.</param>
    /// <param name="genericTypes">Empty array for non-generic use cases.
    /// If a generic type is being parsed, this will contain the generic parameters' types to create an instance of.
    /// E.g. if there is a <c>Foo&lt;T1, T2&gt; : Foo</c> and <typeparamref name="T"/> is <c>Foo</c>,
    /// if the type currently being parsed is <c>Foo&lt;int, string&gt;</c>,
    /// then this parameter will contain <c>Type[] {typeof(int), typeof(string)}</c></param>
    /// <returns>A respective result object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If there aren't enough arguments.
    /// Implementations are advised that they may access arguments by index without prior check, e.g. <c>args[0]</c>,
    /// which will just throw this exception for them if they run out of arguments.</exception>
    new Task<ArgsParseResult<T>> Parse(IImmutableList<string> args, Type[] genericTypes);

    async Task<ArgsParseResult<object>> IArgumentParser.Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        ArgsParseResult<T> parseResult = await Parse(args, genericTypes);
        return parseResult.SuccessResult != null
            ? ArgsParseResult<object>.Success(parseResult.Failures,
                parseResult.SuccessResult.Value.Result!, parseResult.SuccessResult.Value.RemainingArgs)
            : ArgsParseResult<object>.Failure(parseResult.Failures);
    }
}
