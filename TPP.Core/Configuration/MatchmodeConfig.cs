using NodaTime;

namespace TPP.Core.Configuration;

public class MatchmodeConfig : ConfigBase, IRootConfig
{
    public string Schema => "./config.matchmode.schema.json";

    public Duration DefaultBettingDuration { get; init; } = Duration.FromSeconds(120);
    public Duration WarningDuration { get; init; } = Duration.FromSeconds(30);
    public Duration ResultDuration { get; init; } = Duration.FromSeconds(30);
    public int MinimumPokeyen { get; init; } = 200;
    public int SubscriberMinimumPokeyen { get; init; } = 500;
}
