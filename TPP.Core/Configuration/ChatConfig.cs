using System.Collections.Immutable;

namespace TPP.Core.Configuration
{
    /// <summary>
    /// Configurations related to chat communication.
    /// </summary>
    public sealed class ChatConfig : ConfigBase
    {
        // TODO this configurations should probably be in the database instead
        public IImmutableList<string> OperatorNames { get; init; } = ImmutableList.Create("admin");
        public IImmutableList<string> ModeratorNames { get; init; } = ImmutableList<string>.Empty;

        public IImmutableList<ConnectionConfig> Connections { get; init; } =
            ImmutableList.Create<ConnectionConfig>(
                new ConnectionConfig.Console(),
                new ConnectionConfig.Twitch()
            );

        /* whether to forward unprocessed messages to the old core by saving them to the "messagequeue" collection */
        public bool ForwardUnprocessedMessages { get; init; } = true;
    }
}
