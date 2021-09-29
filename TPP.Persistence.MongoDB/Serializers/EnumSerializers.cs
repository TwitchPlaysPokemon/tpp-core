using TPP.Common;

namespace TPP.Persistence.MongoDB.Serializers;

public class BadgeSourceSerializer : EnumToStringUsingTranslationMappingSerializer<Badge.BadgeSource>
{
    public static readonly BadgeSourceSerializer Instance = new();

    private BadgeSourceSerializer() : base(new Dictionary<Badge.BadgeSource, string>
    {
        [Badge.BadgeSource.Pinball] = "pinball",
        [Badge.BadgeSource.RunCaught] = "run_caught",
        [Badge.BadgeSource.Crate] = "crate",
        [Badge.BadgeSource.ManualCreation] = "manual_creation",
        [Badge.BadgeSource.ManualDistribution] = "manual_distribution",
        [Badge.BadgeSource.Breaking] = "breaking",
        [Badge.BadgeSource.Transmutation] = "transmutation",
    })
    {
    }
}

public class RoleSerializer : EnumToStringUsingTranslationMappingSerializer<Role>
{
    public static readonly RoleSerializer Instance = new();

    private RoleSerializer() : base(new Dictionary<Role, string>
    {
        [Role.Operator] = "operator",
        [Role.Moderator] = "moderator",
        [Role.Trusted] = "trusted",
        [Role.MusicTeam] = "musicteam",
        [Role.Developer] = "developer",
        [Role.Artist] = "artist",
        [Role.ModbotExempt] = "modbot_exempt",
    })
    {
    }
}

public class SubscriptionTierSerializer : EnumToStringUsingTranslationMappingSerializer<SubscriptionTier>
{
    public static readonly SubscriptionTierSerializer Instance = new();

    private SubscriptionTierSerializer() : base(new Dictionary<SubscriptionTier, string>
    {
        [SubscriptionTier.Prime] = "Prime",
        [SubscriptionTier.Tier1] = "1000",
        [SubscriptionTier.Tier2] = "2000",
        [SubscriptionTier.Tier3] = "3000",
    })
    {
    }
}

public class GameIdSerializer : EnumToStringUsingEnumMemberValue<GameId>
{
    public static readonly GameIdSerializer Instance = new();
}

public class SwitchingPolicySerializer : EnumToStringUsingEnumMemberValue<SwitchingPolicy>
{
    public static readonly SwitchingPolicySerializer Instance = new();
}
