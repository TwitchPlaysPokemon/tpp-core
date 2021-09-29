using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace TPP.ArgsParsing;

/// <summary>
/// Various extensions to ArgsParser, including type-safe convenience methods
/// </summary>
public static class ArgsParserExtensions
{
    /// <summary>
    /// Try to parse arguments into <typeparamref name="T1"/>
    /// and get the respective <see cref="ArgsParseResult{T}"/>.
    /// This is a type-safe wrapper around <see cref="ArgsParser.ParseRaw"/>.
    /// </summary>
    /// <returns>The respective parse result,
    /// containing an instance of <c>T1</c> on success.</returns>
    public static async Task<ArgsParseResult<T1>> TryParse<T1>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        IEnumerable<Type> types = new[] { typeof(T1) };
        ArgsParseResult<List<object>> parseResult =
            await argsParser.ParseRaw(args, types, errorOnRemainingArgs: true);
        if (parseResult.SuccessResult == null)
            return ArgsParseResult<T1>.Failure(parseResult.Failures);
        Success<List<object>> success = parseResult.SuccessResult.Value;
        return ArgsParseResult<T1>.Success(parseResult.Failures,
            (T1)success.Result[0],
            success.RemainingArgs);
    }

    /// <summary>
    /// Try to parse arguments into <typeparamref name="T1"/> and <typeparamref name="T2"/>
    /// and get the respective <see cref="ArgsParseResult{T}"/>.
    /// This is a type-safe wrapper around <see cref="ArgsParser.ParseRaw"/>.
    /// </summary>
    /// <returns>The respective parse result,
    /// containing a tuple of instances <c>(T1, T2)</c> on success.
    /// </returns>
    public static async Task<ArgsParseResult<(T1, T2)>> TryParse<T1, T2>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        IEnumerable<Type> types = new[] { typeof(T1), typeof(T2) };
        ArgsParseResult<List<object>> parseResult =
            await argsParser.ParseRaw(args, types, errorOnRemainingArgs: true);
        if (parseResult.SuccessResult == null)
            return ArgsParseResult<(T1, T2)>.Failure(parseResult.Failures);
        Success<List<object>> success = parseResult.SuccessResult.Value;
        return ArgsParseResult<(T1, T2)>.Success(parseResult.Failures,
            ((T1)success.Result[0], (T2)success.Result[1]),
            success.RemainingArgs);
    }

    /// <summary>
    /// Try to parse arguments into <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>
    /// and get the respective <see cref="ArgsParseResult{T}"/>.
    /// This is a type-safe wrapper around <see cref="ArgsParser.ParseRaw"/>.
    /// </summary>
    /// <returns>The respective parse result,
    /// containing a tuple of instances <c>(T1, T2, T3)</c> on success.
    /// </returns>
    public static async Task<ArgsParseResult<(T1, T2, T3)>> TryParse<T1, T2, T3>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        IEnumerable<Type> types = new[] { typeof(T1), typeof(T2), typeof(T3) };
        ArgsParseResult<List<object>> parseResult =
            await argsParser.ParseRaw(args, types, errorOnRemainingArgs: true);
        if (parseResult.SuccessResult == null)
            return ArgsParseResult<(T1, T2, T3)>.Failure(parseResult.Failures);
        Success<List<object>> success = parseResult.SuccessResult.Value;
        return ArgsParseResult<(T1, T2, T3)>.Success(parseResult.Failures,
            ((T1)success.Result[0], (T2)success.Result[1], (T3)success.Result[2]),
            success.RemainingArgs);
    }

    /// <summary>
    /// Try to parse arguments into <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>,
    /// <typeparamref name="T4"/> and get the respective <see cref="ArgsParseResult{T}"/>.
    /// This is a type-safe wrapper around <see cref="ArgsParser.ParseRaw"/>.
    /// </summary>
    /// <returns>The respective parse result,
    /// containing a tuple of instances <c>(T1, T2, T3, T4)</c> on success.
    /// </returns>
    public static async Task<ArgsParseResult<(T1, T2, T3, T4)>> TryParse<T1, T2, T3, T4>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        IEnumerable<Type> types = new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
        ArgsParseResult<List<object>> parseResult =
            await argsParser.ParseRaw(args, types, errorOnRemainingArgs: true);
        if (parseResult.SuccessResult == null)
            return ArgsParseResult<(T1, T2, T3, T4)>.Failure(parseResult.Failures);
        Success<List<object>> success = parseResult.SuccessResult.Value;
        return ArgsParseResult<(T1, T2, T3, T4)>.Success(parseResult.Failures,
            ((T1)success.Result[0], (T2)success.Result[1], (T3)success.Result[2], (T4)success.Result[3]),
            success.RemainingArgs);
    }

    /// <summary>
    /// Try to parse arguments into <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>,
    /// <typeparamref name="T4"/>, <typeparamref name="T5"/> and get the respective <see cref="ArgsParseResult{T}"/>.
    /// This is a type-safe wrapper around <see cref="ArgsParser.ParseRaw"/>.
    /// </summary>
    /// <returns>The respective parse result,
    /// containing a tuple of instances <c>(T1, T2, T3, T4, T5)</c> on success.
    /// </returns>
    public static async Task<ArgsParseResult<(T1, T2, T3, T4, T5)>> TryParse<T1, T2, T3, T4, T5>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        IEnumerable<Type> types = new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
        ArgsParseResult<List<object>> parseResult =
            await argsParser.ParseRaw(args, types, errorOnRemainingArgs: true);
        if (parseResult.SuccessResult == null)
            return ArgsParseResult<(T1, T2, T3, T4, T5)>.Failure(parseResult.Failures);
        Success<List<object>> success = parseResult.SuccessResult.Value;
        return ArgsParseResult<(T1, T2, T3, T4, T5)>.Success(parseResult.Failures,
            ((T1)success.Result[0], (T2)success.Result[1], (T3)success.Result[2], (T4)success.Result[3],
                (T5)success.Result[4]),
            success.RemainingArgs);
    }

    /// Wrapper around <see cref="TryParse{T1}"/> to directly get the result (not wrapped in a
    /// <see cref="ArgsParseResult{T}"/>) or throw an <see cref="ArgsParseFailure"/> exception if parsing failed.
    public static async Task<T1> Parse<T1>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        ArgsParseResult<T1> parseResult = await argsParser.TryParse<T1>(args);
        if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.Failures);
        return parseResult.SuccessResult.Value.Result;
    }

    /// Wrapper around <see cref="TryParse{T1,T2}"/> to directly get the result (not wrapped in a
    /// <see cref="ArgsParseResult{T}"/>) or throw an <see cref="ArgsParseFailure"/> exception if parsing failed.
    public static async Task<(T1, T2)> Parse<T1, T2>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        ArgsParseResult<(T1, T2)> parseResult = await argsParser.TryParse<T1, T2>(args);
        if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.Failures);
        return parseResult.SuccessResult.Value.Result;
    }

    /// Wrapper around <see cref="TryParse{T1,T2,T3}"/> to directly get the result (not wrapped in a
    /// <see cref="ArgsParseResult{T}"/>) or throw an <see cref="ArgsParseFailure"/> exception if parsing failed.
    public static async Task<(T1, T2, T3)> Parse<T1, T2, T3>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        ArgsParseResult<(T1, T2, T3)> parseResult = await argsParser.TryParse<T1, T2, T3>(args);
        if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.Failures);
        return parseResult.SuccessResult.Value.Result;
    }

    /// Wrapper around <see cref="TryParse{T1,T2,T3,T4}"/> to directly get the result (not wrapped in a
    /// <see cref="ArgsParseResult{T}"/>) or throw an <see cref="ArgsParseFailure"/> exception if parsing failed.
    public static async Task<(T1, T2, T3, T4)> Parse<T1, T2, T3, T4>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        ArgsParseResult<(T1, T2, T3, T4)> parseResult = await argsParser.TryParse<T1, T2, T3, T4>(args);
        if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.Failures);
        return parseResult.SuccessResult.Value.Result;
    }

    /// Wrapper around <see cref="TryParse{T1,T2,T3,T4,T5}"/> to directly get the result (not wrapped in a
    /// <see cref="ArgsParseResult{T}"/>) or throw an <see cref="ArgsParseFailure"/> exception if parsing failed.
    public static async Task<(T1, T2, T3, T4, T5)> Parse<T1, T2, T3, T4, T5>(
        this ArgsParser argsParser, IImmutableList<string> args)
    {
        ArgsParseResult<(T1, T2, T3, T4, T5)> parseResult = await argsParser.TryParse<T1, T2, T3, T4, T5>(args);
        if (parseResult.SuccessResult == null) throw new ArgsParseFailure(parseResult.Failures);
        return parseResult.SuccessResult.Value.Result;
    }
}
