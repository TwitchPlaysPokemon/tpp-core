using System.Collections.Immutable;
using TPP.ArgsParsing;

namespace TPP.Core.Commands;

public readonly record struct CommandContext(
    Message Message,
    IImmutableList<string> Args,
    ArgsParser ArgsParser);
