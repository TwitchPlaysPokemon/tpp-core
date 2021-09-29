using System.Collections.Immutable;

namespace TPP.Core.Configuration;

/// <summary>
/// Configurations related to chat communication.
/// </summary>
public sealed class ChatConfig : ConfigBase
{
    public IImmutableList<string> DefaultOperatorNames { get; init; } = ImmutableList.Create("admin");

    public IImmutableList<ConnectionConfig> Connections { get; init; } =
        ImmutableList.Create<ConnectionConfig>(
            new ConnectionConfig.Console(),
            new ConnectionConfig.Twitch(),
            new ConnectionConfig.Simulation()
        );

    /* whether to forward unprocessed messages to the old core by saving them to the "messagequeue" collection */
    public bool ForwardUnprocessedMessages { get; init; } = true;
}
