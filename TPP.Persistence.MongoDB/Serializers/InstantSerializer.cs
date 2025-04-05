using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NodaTime;

namespace TPP.Persistence.MongoDB.Serializers;

public class InstantSerializer : SerializerBase<Instant>
{
    public static readonly InstantSerializer Instance = new();

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
