using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NodaTime;

namespace Persistence.MongoDB.Serializers
{
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
                BsonType.Null => default,
                BsonType.DateTime => Instant.FromUnixTimeMilliseconds(context.Reader.ReadDateTime()),
                _ => throw new NotSupportedException($"Cannot convert type '{type}' to Instant.")
            };
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Instant value)
        {
            if (value == null) context.Writer.WriteNull();
            else context.Writer.WriteDateTime(value.ToUnixTimeMilliseconds());
        }
    }
}
