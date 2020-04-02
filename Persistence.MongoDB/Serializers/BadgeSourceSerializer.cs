using System.Collections.Generic;
using Persistence.Models;

namespace Persistence.MongoDB.Serializers
{
    public class BadgeSourceSerializer : EnumToStringUsingTranslationMappingSerializer<Badge.BadgeSource>
    {
        public static readonly BadgeSourceSerializer Instance = new BadgeSourceSerializer();

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
}
