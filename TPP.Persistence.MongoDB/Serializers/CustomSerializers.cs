namespace TPP.Persistence.MongoDB.Serializers;

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

        // TODO workaround for https://jira.mongodb.org/browse/CSHARP-3449
        // Remove this custom bson mapper once the mongodb linq driver is able to use the serializers for converting
        // types to bson. That can be checked by removing this line and seeing if the badge repo tests pass.
        BsonTypeMapper.RegisterCustomTypeMapper(typeof(Instant), InstantBsonTypeMapper.Instance);
    }
}
