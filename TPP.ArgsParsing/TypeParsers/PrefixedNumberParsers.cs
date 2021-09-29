using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TPP.ArgsParsing.Types;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser capable recognizing numbers prefixed with a predefined prefix.
/// Since each parser is bound to a fixed type you need to declare a custom class for the numeric type
/// you want to parse. That class needs to inherit <see cref="ImplicitNumber"/>, for example:
/// <code>
/// public class Pokeyen : ImplicitNumber { }
/// </code>
/// You would register a parser for this type as follows:
/// <code>
/// argsParser.AddArgumentParser(new PrefixedNumberParser&lt;Pokeyen&gt;("P"));
/// </code>
/// Since <see cref="ImplicitNumber"/> is implicitly convertible to <c>int</c>,
/// you can assign the result to an int, for example:
/// <code>
/// int pokeyen = argsParser.Parse&lt;Pokeyen&gt;(...);
/// </code>
/// </summary>
public class PrefixedNumberParser<T> : IArgumentParser<T> where T : ImplicitNumber, new()
{
    private readonly string _prefix;
    private readonly int _minValue;
    private readonly int _maxValue;
    private readonly Regex _regex;

    protected PrefixedNumberParser(
        string prefix,
        int minValue = 0,
        int maxValue = int.MaxValue,
        bool caseSensitive = false)
    {
        _prefix = prefix;
        _minValue = minValue;
        _maxValue = maxValue;
        var options = RegexOptions.Compiled;
        if (!caseSensitive) options |= RegexOptions.IgnoreCase;
        _regex = new Regex(@$"^{Regex.Escape(prefix)}(?<number>[+-]?[0-9]+)$", options);
    }

    public Task<ArgsParseResult<T>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        string str = args[0];
        Match match = _regex.Match(str);
        if (!match.Success)
        {
            return Task.FromResult(ArgsParseResult<T>.Failure(
                $"did not recognize '{str}' as a '{_prefix}'-prefixed number"));
        }
        try
        {
            int number = int.Parse(match.Groups["number"].Value);
            if (number < _minValue)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure(
                    $"'{str}' cannot be less than {_prefix}{_minValue}", ErrorRelevanceConfidence.Likely));
            }
            if (number > _maxValue)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure(
                    $"'{str}' cannot be more than {_prefix}{_maxValue}", ErrorRelevanceConfidence.Likely));
            }
            var value = new T { Number = number };
            return Task.FromResult(ArgsParseResult<T>.Success(value, args.Skip(1).ToImmutableList()));
        }
        catch (OverflowException)
        {
            return Task.FromResult(ArgsParseResult<T>.Failure(
                $"'{str}' is out of range", ErrorRelevanceConfidence.Likely));
        }
    }
}

public class PokeyenParser : PrefixedNumberParser<Pokeyen>
{
    public PokeyenParser() : base("P")
    {
    }
}

public class TokensParser : PrefixedNumberParser<Tokens>
{
    public TokensParser() : base("T")
    {
    }
}

public class SignedPokeyenParser : PrefixedNumberParser<SignedPokeyen>
{
    public SignedPokeyenParser() : base("P", minValue: int.MinValue)
    {
    }
}

public class SignedTokensParser : PrefixedNumberParser<SignedTokens>
{
    public SignedTokensParser() : base("T", minValue: int.MinValue)
    {
    }
}
