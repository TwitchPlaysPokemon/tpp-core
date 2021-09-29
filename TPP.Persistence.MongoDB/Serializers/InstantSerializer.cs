using MongoDB.Bson.Serialization.Serializers;

namespace TPP.Persistence.MongoDB.Serializers;

public class InstantSerializer : SerializerBase<Instant>
{
    public static readonly InstantSerializer Instance = new InstantSerializer();

    private InstantSerializer()
    {
    }

    public override Instant Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        BsonType type = context.Reader.GetCurrentBsonType();
        return type switch
        {
            BsonType.DateTime => Instant.FromUnixTimeMilliseconds(context.Reader.ReadDateTime()),
            _ => throw CreateCannotBeDeserializedException()
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Instant value)
    {
        context.Writer.WriteDateTime(value.ToUnixTimeMilliseconds());
    }
}

// TODO workaround for https://jira.mongodb.org/browse/CSHARP-3449
// remove this class once that bug is fixed
public class InstantBsonTypeMapper : ICustomBsonTypeMapper
{
    public static readonly InstantBsonTypeMapper Instance = new();

    private InstantBsonTypeMapper()
    {
    }

    public bool TryMapToBsonValue(object value, out BsonValue bsonValue)
    {
        if (value is Instant instant)
        {
            bsonValue = new BsonDateTime(instant.ToDateTimeUtc());
            return true;
        }
        else
        {
            bsonValue = default!;
            return false;
        }
    }
}
