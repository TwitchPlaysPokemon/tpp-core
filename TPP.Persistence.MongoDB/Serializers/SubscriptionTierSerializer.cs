using System.Collections.Generic;
using TPP.Common;

namespace TPP.Persistence.MongoDB.Serializers
{
    public class SubscriptionTierSerializer : EnumToStringUsingTranslationMappingSerializer<SubscriptionTier>
    {
        public static readonly SubscriptionTierSerializer Instance = new();

        public SubscriptionTierSerializer() : base(new Dictionary<SubscriptionTier, string>
        {
            [SubscriptionTier.Prime] = "Prime",
            [SubscriptionTier.Tier1] = "1000",
            [SubscriptionTier.Tier2] = "2000",
            [SubscriptionTier.Tier3] = "3000",
        })
        {
        }
    }
}
