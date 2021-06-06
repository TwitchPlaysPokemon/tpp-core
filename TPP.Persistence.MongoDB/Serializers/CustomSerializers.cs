using MongoDB.Bson.Serialization;

namespace TPP.Persistence.MongoDB.Serializers
{
    /// <summary>
    /// Offers a convenient way to register all serializers that should be globally registered
    /// for their respective type.
    /// </summary>
    public static class CustomSerializers
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;
            BsonSerializer.RegisterSerializer(BadgeSourceSerializer.Instance);
            BsonSerializer.RegisterSerializer(PkmnSpeciesSerializer.Instance);
            BsonSerializer.RegisterSerializer(InstantSerializer.Instance);
            BsonSerializer.RegisterSerializer(NullableInstantSerializer.Instance);
            BsonSerializer.RegisterSerializer(SubscriptionTierSerializer.Instance);
            BsonSerializer.RegisterSerializer(RoleSerializer.Instance);
            BsonSerializer.RegisterSerializer(GameIdSerializer.Instance);
            BsonSerializer.RegisterSerializer(SwitchingPolicySerializer.Instance);
            BsonSerializer.RegisterSerializer(MatchResultSerializer.Instance);
        }
    }
}
