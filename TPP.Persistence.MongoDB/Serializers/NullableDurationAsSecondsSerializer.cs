using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NodaTime;

namespace TPP.Persistence.MongoDB.Serializers;

/// <summary>
/// Purposefully not registered as a global serializer in <see cref="CustomSerializers"/>,
/// because it truncates to seconds.
/// What precision to save should be a decision best made per field and not globally.
/// </summary>
public class NullableDurationAsSecondsSerializer : SerializerBase<Duration?>
{
    public static readonly NullableDurationAsSecondsSerializer Instance = new();

    private NullableDurationAsSecondsSerializer()
    {
    }

    public override Duration? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        BsonType type = context.Reader.GetCurrentBsonType();
        if (type == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        if (type == BsonType.Int32)
        {
            return Duration.FromSeconds(context.Reader.ReadInt32());
        }
        throw CreateCannotBeDeserializedException();
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Duration? value)
    {
        if (value == null) context.Writer.WriteNull();
        else context.Writer.WriteInt32((int)value.Value.TotalSeconds);
    }
}
