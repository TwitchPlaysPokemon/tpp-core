namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser that just forwards one argument as a string.
/// Parsing always succeeds, given that the arguments aren't exhausted.
/// </summary>
public class StringParser : IArgumentParser<string>
{
    public Task<ArgsParseResult<string>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        ArgsParseResult<string> result = ArgsParseResult<string>.Success(args[0], args.Skip(1).ToImmutableList());
        return Task.FromResult(result);
    }
}
