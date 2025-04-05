using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NodaTime;

namespace TPP.Persistence.MongoDB.Serializers;

public class NullableInstantSerializer : SerializerBase<Instant?>
{
    public static readonly NullableInstantSerializer Instance = new NullableInstantSerializer();

    private NullableInstantSerializer()
    {
    }

    public override Instant? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        BsonType type = context.Reader.GetCurrentBsonType();
        if (type == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        if (type == BsonType.DateTime)
        {
            return Instant.FromUnixTimeMilliseconds(context.Reader.ReadDateTime());
        }
        throw CreateCannotBeDeserializedException();
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Instant? value)
    {
        if (value == null) context.Writer.WriteNull();
        else context.Writer.WriteDateTime(value.Value.ToUnixTimeMilliseconds());
    }
}
