using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ArgsParsing
{
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
    /// Custom types can be specified by inheriting from <see cref="BaseArgumentParser{T}"/> and registering
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
            var allRemainingArgs = args;
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
                Debug.Assert(parser != null, "try-get succeeded and the dictionary does not contain null values");
                Type[] genericTypes = type.IsGenericType ? type.GenericTypeArguments : new Type[] { };
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
                if (parseResult.FailureResult != null)
                {
                    failures.Add(parseResult.FailureResult.Value);
                }
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
            if (success)
            {
                return ArgsParseResult<List<object>>.Success(null, results, allRemainingArgs);
            }
            else
            {
                Failure mostRelevantFailure = failures.OrderByDescending(failure => failure.Relevance).First();
                return ArgsParseResult<List<object>>.Failure(mostRelevantFailure);
            }
        }

        #region type-safe convenience methods

        /// <summary>
        /// Try to parse arguments into <typeparamref name="T1"/>
        /// and get the respective <see cref="ArgsParseResult{T}"/>.
        /// This is a type-safe wrapper around <see cref="ParseRaw"/>.
        /// </summary>
        /// <param name="args">List of string arguments to parse from.</param>
        /// <typeparam name="T1">Type to attempt to create an instance of.</typeparam>
        /// <returns>The respective parse result,
        /// containing an instance of <c>T1</c> on success.</returns>
        public async Task<ArgsParseResult<T1>> TryParse<T1>(IImmutableList<string> args)
        {
            IEnumerable<Type> types = new[] {typeof(T1)};
            ArgsParseResult<List<object>> parseResult = await ParseRaw(args, types, errorOnRemainingArgs: true);
            if (parseResult.SuccessResult == null)
                return ArgsParseResult<T1>.Failure(parseResult.FailureResult);
            Success<List<object>> success = parseResult.SuccessResult.Value;
            return ArgsParseResult<T1>.Success(parseResult.FailureResult,
                (T1) success.Result[0],
                success.RemainingArgs);
        }

        /// <summary>
        /// Try to parse arguments into <typeparamref name="T1"/> and <typeparamref name="T2"/>
        /// and get the respective <see cref="ArgsParseResult{T}"/>.
        /// This is a type-safe wrapper around <see cref="ParseRaw"/>.
        /// </summary>
        /// <param name="args">List of string arguments to parse from.</param>
        /// <typeparam name="T1">First type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T2">Second type to attempt to create an instance of.</typeparam>
        /// <returns>The respective parse result,
        /// containing a tuple of instances <c>(T1, T2)</c> on success.
        /// </returns>
        public async Task<ArgsParseResult<(T1, T2)>> TryParse<T1, T2>(IImmutableList<string> args)
        {
            IEnumerable<Type> types = new[] {typeof(T1), typeof(T2)};
            ArgsParseResult<List<object>> parseResult = await ParseRaw(args, types, errorOnRemainingArgs: true);
            if (parseResult.SuccessResult == null)
                return ArgsParseResult<(T1, T2)>.Failure(parseResult.FailureResult);
            Success<List<object>> success = parseResult.SuccessResult.Value;
            return ArgsParseResult<(T1, T2)>.Success(parseResult.FailureResult,
                ((T1) success.Result[0], (T2) success.Result[1]),
                success.RemainingArgs);
        }

        /// <summary>
        /// Try to parse arguments into <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>
        /// and get the respective <see cref="ArgsParseResult{T}"/>.
        /// This is a type-safe wrapper around <see cref="ParseRaw"/>.
        /// </summary>
        /// <param name="args">List of string arguments to parse from.</param>
        /// <typeparam name="T1">First type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T2">Second type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T3">Third type to attempt to create an instance of.</typeparam>
        /// <returns>The respective parse result,
        /// containing a tuple of instances <c>(T1, T2, T3)</c> on success.
        /// </returns>
        public async Task<ArgsParseResult<(T1, T2, T3)>> TryParse<T1, T2, T3>(IImmutableList<string> args)
        {
            IEnumerable<Type> types = new[] {typeof(T1), typeof(T2), typeof(T3)};
            ArgsParseResult<List<object>> parseResult = await ParseRaw(args, types, errorOnRemainingArgs: true);
            if (parseResult.SuccessResult == null)
                return ArgsParseResult<(T1, T2, T3)>.Failure(parseResult.FailureResult);
            Success<List<object>> success = parseResult.SuccessResult.Value;
            return ArgsParseResult<(T1, T2, T3)>.Success(parseResult.FailureResult,
                ((T1) success.Result[0], (T2) success.Result[1], (T3) success.Result[2]),
                success.RemainingArgs);
        }

        /// <summary>
        /// Try to parse arguments into <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>,
        /// <typeparamref name="T4"/> and get the respective <see cref="ArgsParseResult{T}"/>.
        /// This is a type-safe wrapper around <see cref="ParseRaw"/>.
        /// </summary>
        /// <param name="args">List of string arguments to parse from.</param>
        /// <typeparam name="T1">First type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T2">Second type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T3">Third type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T4">Fourth type to attempt to create an instance of.</typeparam>
        /// <returns>The respective parse result,
        /// containing a tuple of instances <c>(T1, T2, T3, T4)</c> on success.
        /// </returns>
        public async Task<ArgsParseResult<(T1, T2, T3, T4)>> TryParse<T1, T2, T3, T4>(IImmutableList<string> args)
        {
            IEnumerable<Type> types = new[] {typeof(T1), typeof(T2), typeof(T3), typeof(T4)};
            ArgsParseResult<List<object>> parseResult = await ParseRaw(args, types, errorOnRemainingArgs: true);
            if (parseResult.SuccessResult == null)
                return ArgsParseResult<(T1, T2, T3, T4)>.Failure(parseResult.FailureResult);
            Success<List<object>> success = parseResult.SuccessResult.Value;
            return ArgsParseResult<(T1, T2, T3, T4)>.Success(parseResult.FailureResult,
                ((T1) success.Result[0], (T2) success.Result[1], (T3) success.Result[2], (T4) success.Result[3]),
                success.RemainingArgs);
        }

        /// <summary>
        /// Wrapper around <see cref="TryParse{T1}"/> to directly get the result (not wrapped in a
        /// <see cref="ArgsParseResult{T}"/>) or throw an exception if parsing failed.
        /// </summary>
        /// <param name="args">List of string arguments to parse from.</param>
        /// <typeparam name="T1">Type to attempt to create an instance of.</typeparam>
        /// <returns>An instance of <c>T1</c></returns>
        /// <exception cref="ArgsParseFailure">If parsing failed</exception>
        public async Task<T1> Parse<T1>(IImmutableList<string> args)
        {
            ArgsParseResult<T1> parseResult = await TryParse<T1>(args);
            if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.FailureResult?.Error);
            return parseResult.SuccessResult.Value.Result;
        }

        /// <summary>
        /// Wrapper around <see cref="TryParse{T1,T2}"/> to directly get the result (not wrapped in a
        /// <see cref="ArgsParseResult{T}"/>) or throw an exception if parsing failed.
        /// </summary>
        /// <param name="args">List of string arguments to parse from.</param>
        /// <typeparam name="T1">First type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T2">Second type to attempt to create an instance of.</typeparam>
        /// <returns>A tuple of instances <c>(T1, T2)</c></returns>
        /// <exception cref="ArgsParseFailure">If parsing failed</exception>
        public async Task<(T1, T2)> Parse<T1, T2>(IImmutableList<string> args)
        {
            ArgsParseResult<(T1, T2)> parseResult = await TryParse<T1, T2>(args);
            if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.FailureResult?.Error);
            return parseResult.SuccessResult.Value.Result;
        }

        /// <summary>
        /// Wrapper around <see cref="TryParse{T1,T2,T3}"/> to directly get the result (not wrapped in a
        /// <see cref="ArgsParseResult{T}"/>) or throw an exception if parsing failed.
        /// </summary>
        /// <param name="args">List of string arguments to parse from.</param>
        /// <typeparam name="T1">First type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T2">Second type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T3">Third type to attempt to create an instance of.</typeparam>
        /// <returns>A tuple of instances <c>(T1, T2, T3)</c></returns>
        /// <exception cref="ArgsParseFailure">If parsing failed</exception>
        public async Task<(T1, T2, T3)> Parse<T1, T2, T3>(IImmutableList<string> args)
        {
            ArgsParseResult<(T1, T2, T3)> parseResult = await TryParse<T1, T2, T3>(args);
            if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.FailureResult?.Error);
            return parseResult.SuccessResult.Value.Result;
        }

        /// <summary>
        /// Wrapper around <see cref="TryParse{T1,T2,T3,T4}"/> to directly get the result (not wrapped in a
        /// <see cref="ArgsParseResult{T}"/>) or throw an exception if parsing failed.
        /// </summary>
        /// <param name="args">List of string arguments to parse from.</param>
        /// <typeparam name="T1">First type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T2">Second type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T3">Third type to attempt to create an instance of.</typeparam>
        /// <typeparam name="T4">Fourth type to attempt to create an instance of.</typeparam>
        /// <returns>A tuple of instances <c>(T1, T2, T3, T4)</c></returns>
        /// <exception cref="ArgsParseFailure">If parsing failed</exception>
        public async Task<(T1, T2, T3, T4)> Parse<T1, T2, T3, T4>(IImmutableList<string> args)
        {
            ArgsParseResult<(T1, T2, T3, T4)> parseResult = await TryParse<T1, T2, T3, T4>(args);
            if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.FailureResult?.Error);
            return parseResult.SuccessResult.Value.Result;
        }

        #endregion
    }
}
