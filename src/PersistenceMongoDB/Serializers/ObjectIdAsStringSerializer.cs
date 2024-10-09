using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace TPP.Persistence.MongoDB.Serializers;

/// <summary>
/// A serializer for representing Ids as <see cref="ObjectId"/> in the database but as <see cref="string"/> in code.
/// </summary>
public class ObjectIdAsStringSerializer : SerializerBase<string>, IRepresentationConfigurable
{
    public static readonly ObjectIdAsStringSerializer Instance = new();

    public BsonType Representation => BsonType.ObjectId;

    public IBsonSerializer WithRepresentation(BsonType representation) => throw new NotSupportedException();

    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return context.Reader.ReadObjectId().ToString();
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        context.Writer.WriteObjectId(ObjectId.Parse(value));
    }
}
