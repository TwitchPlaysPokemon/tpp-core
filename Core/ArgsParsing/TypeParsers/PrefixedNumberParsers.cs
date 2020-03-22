using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.ArgsParsing.Types;

namespace Core.ArgsParsing.TypeParsers
{
    public class PrefixedNumberParser<T> : BaseArgumentParser<T> where T : ImplicitNumber, new()
    {
        private readonly Regex _regex;

        protected PrefixedNumberParser(string prefix, bool caseSensitive = false)
        {
            var options = RegexOptions.Compiled;
            if (!caseSensitive) options |= RegexOptions.IgnoreCase;
            _regex = new Regex(@$"^{Regex.Escape(prefix)}(?<number>\d+)$", options);
        }

        public override Task<ArgsParseResult<T>> Parse(IReadOnlyCollection<string> args, Type[] genericTypes)
        {
            var match = _regex.Match(args.First());
            if (!match.Success)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure());
            }
            try
            {
                var value = new T {Number = int.Parse(match.Groups["number"].Value)};
                return Task.FromResult(ArgsParseResult<T>.Success(value, args.Skip(1).ToImmutableList()));
            }
            catch (FormatException)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure());
            }
            catch (OverflowException)
            {
                return Task.FromResult(ArgsParseResult<T>.Failure());
            }
        }
    }

    public class PokeyenParser : PrefixedNumberParser<Pokeyen>
    {
        public PokeyenParser() : base("P") { }
    }

    public class TokensParser : PrefixedNumberParser<Tokens>
    {
        public TokensParser() : base("T") { }
    }

}
