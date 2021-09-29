using System.Collections.Immutable;
using TPP.ArgsParsing;

namespace TPP.Core.Commands;

public readonly struct CommandContext
{
    public Message Message { get; }
    public IImmutableList<string> Args { get; }
    public ArgsParser ArgsParser { get; }

    public CommandContext(Message message, IImmutableList<string> args, ArgsParser argsParser)
    {
        Message = message;
        Args = args;
        ArgsParser = argsParser;
    }
}
