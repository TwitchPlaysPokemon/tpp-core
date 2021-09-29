using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace TPP.ArgsParsing;

public class MissingParserException : ArgumentException
{
    public Type TypeWithoutParser { get; }

    public MissingParserException(Type typeWithoutParser) : base($"No parser found for type {typeWithoutParser}")
    {
        TypeWithoutParser = typeWithoutParser;
    }
}

/// <summary>
/// This class aims to remove boilerplate argument parsing code in e.g. chat command handlers
/// by letting implementations simply declare what arguments they expect.
/// A simple usage may look like this:
/// <code>
/// (var user, int pokeyen) = await argsParser.Parse&lt;User, Pokeyen&gt;(args);
/// </code>
/// The above code will abort if parsing failed by throwing an <see cref="ArgsParseFailure"/>.
/// If more control over parsing failures is required, this could be achieved as follows:
/// <code>
/// var parseResult = await argsParser.TryParse&lt;User, Pokeyen&gt;(args);
/// if (parseResult.SuccessResult != null)
/// {
///     (var user, int pokeyen) = parseResult.SuccessResult.Value.Result;
///     // success
/// }
/// else
/// {
///     var failure = parseResult.FailureResult?.Error;
///     // failure
/// }
/// </code>
/// Custom types can be specified by inheriting from <see cref="IArgumentParser{T}"/> and registering
/// that parser using <see cref="AddArgumentParser{T}"/>.
/// </summary>
public class ArgsParser
{
    private readonly Dictionary<Type, IArgumentParser> _parsers = new Dictionary<Type, IArgumentParser>();

    /// <summary>
    /// Adds an argument parser instance capable of parsing a specific type.
    /// </summary>
    /// <param name="argumentParser"></param>
    /// <typeparam name="T"></typeparam>
    public void AddArgumentParser<T>(IArgumentParser<T> argumentParser)
    {
        _parsers.Add(typeof(T), argumentParser);
    }

    public bool RemoveArgumentParser<T>()
    {
        return _parsers.Remove(typeof(T));
    }

    /// <summary>
    /// For a list of strings, tries to parse those strings into instance of the supplied types.
    /// </summary>
    /// <param name="args">The input to be parsed, supplied as a list of string arguments</param>
    /// <param name="types">The types of which instances will be attempted to be created from the input.</param>
    /// <param name="errorOnRemainingArgs">Whether it is an error if the arguments weren't fully consumed.</param>
    /// <returns>A parsing result object. On success that will contain a list of objects with the same length
    /// and same types as supplied in <paramref name="types"/></returns>
    /// <exception cref="MissingParserException">If there was no parser found for one of the supplied types.
    /// You must register a parser using <see cref="AddArgumentParser{T}"/> for each type.</exception>
    /// <exception cref="InvalidOperationException">If the requested types cannot be parsed,
    /// because their types or respective parsers are not implemented correctly.</exception>
    public async Task<ArgsParseResult<List<object>>> ParseRaw(
        IImmutableList<string> args,
        IEnumerable<Type> types,
        bool errorOnRemainingArgs = false)
    {
        IImmutableList<string> allRemainingArgs = args;
        var results = new List<object>();
        bool success = true;
        var failures = new List<Failure>();
        foreach (var type in types)
        {
            Type? queryType = type.IsGenericType ? type.BaseType : type;
            if (queryType == null || queryType.IsGenericType)
            {
                throw new InvalidOperationException($"generic type {type} need a non-generic base type");
            }
            if (!_parsers.TryGetValue(queryType, out IArgumentParser? parser))
            {
                throw new MissingParserException(typeWithoutParser: type);
            }
            Type[] genericTypes = type.IsGenericType ? type.GenericTypeArguments : Array.Empty<Type>();
            ArgsParseResult<object> parseResult;
            try
            {
                parseResult = await parser.Parse(allRemainingArgs, genericTypes);
            }
            catch (ArgumentOutOfRangeException)
            {
                failures.Add(new Failure(ErrorRelevanceConfidence.Unlikely, "too few arguments"));
                success = false;
                break;
            }
            failures.AddRange(parseResult.Failures);
            if (parseResult.SuccessResult != null)
            {
                Success<object> successResult = parseResult.SuccessResult.Value;
                results.Add(successResult.Result);
                allRemainingArgs = successResult.RemainingArgs;
            }
            else
            {
                success = false;
                break;
            }
        }
        if (success && errorOnRemainingArgs && allRemainingArgs.Any())
        {
            success = false;
            failures.Add(new Failure(ErrorRelevanceConfidence.Unlikely, "too many arguments"));
        }
        return success
            ? ArgsParseResult<List<object>>.Success(failures.ToImmutableList(), results, allRemainingArgs)
            : ArgsParseResult<List<object>>.Failure(failures.ToImmutableList());
    }
}
