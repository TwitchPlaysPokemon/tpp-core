using System;
using System.Collections.Immutable;
using System.Linq;

namespace Core.Configuration
{
    /// <summary>
    /// Configurations related to IRC chat communication.
    /// </summary>
    // properties need setters for deserialization
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    public sealed class IrcConfig : ConfigBase
    {
        /* connection information */
        public string Channel { get; private set; } = "twitchplayspokemon";

        /* account information */
        public string Username { get; private set; } = "justinfan27365461784";
        public string Password { get; private set; } = "oauth:mysecret";

        // TODO this configurations should probably be in the database instead
        public IImmutableList<string> OperatorNames { get; private set; } = ImmutableList<string>.Empty;

        /* communication settings */
        public enum SuppressionType
        {
            Whisper,
            Message,
            Command
        }
        public ImmutableHashSet<SuppressionType> Suppressions { get; private set; }
            = Enum.GetValues(typeof(SuppressionType)).Cast<SuppressionType>().ToImmutableHashSet(); // all by default
        // list of usernames and channels that may receive outbound messages even with suppression enabled
        public ImmutableHashSet<string> SuppressionOverrides { get; private set; } = ImmutableHashSet.Create<string>();
    }
}
