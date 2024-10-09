using System.Collections.Immutable;
using TPP.ArgsParsing;
using TPP.Core.Chat;

namespace TPP.Core.Commands;

public readonly record struct CommandContext(
    Message Message,
    IImmutableList<string> Args,
    ArgsParser ArgsParser,
    IChat? Source = null);
