using System;
using System.Collections.Immutable;
using System.Linq;

namespace Core.Configuration
{
    /// <summary>
    /// Configurations related to IRC chat communication.
    /// </summary>
    public sealed class IrcConfig : ConfigBase
    {
        /* connection information */
        public string Channel { get; init; } = "twitchplayspokemon";

        /* account information */
        public string Username { get; init; } = "justinfan27365461784";
        public string Password { get; init; } = "oauth:mysecret";

        /* communication settings */
        public enum SuppressionType
        {
            Whisper,
            Message,
            Command
        }
        public ImmutableHashSet<SuppressionType> Suppressions { get; init; }
            = Enum.GetValues(typeof(SuppressionType)).Cast<SuppressionType>().ToImmutableHashSet(); // all by default
        // list of usernames and channels that may receive outbound messages even with suppression enabled
        public ImmutableHashSet<string> SuppressionOverrides { get; init; } = ImmutableHashSet.Create<string>();
    }
}
