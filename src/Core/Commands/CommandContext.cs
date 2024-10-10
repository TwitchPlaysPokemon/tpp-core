using System.Collections.Immutable;
using ArgsParsing;
using Core.Chat;

namespace Core.Commands;

public readonly record struct CommandContext(
    Message Message,
    IImmutableList<string> Args,
    ArgsParser ArgsParser,
    IChat? Source = null);
