using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace TPP.Persistence.MongoDB.Serializers
{
    /// <summary>
    /// A serializer for representing a list of Ids as <see cref="ObjectId"/> in the database but as <see cref="string"/> in code.
    /// </summary>
    public class ObjectIdListAsStringSerializer : SerializerBase<IReadOnlyList<string>>
    {
        public static readonly ObjectIdListAsStringSerializer Instance = new();

        public override IReadOnlyList<string> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            List<string> list = new();
            while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
                list.Add(context.Reader.ReadObjectId().ToString());
            context.Reader.ReadEndArray();
            return list;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IReadOnlyList<string> values)
        {
            context.Writer.WriteStartArray();
            foreach (string value in values)
                context.Writer.WriteObjectId(ObjectId.Parse(value));
            context.Writer.WriteEndArray();
        }
    }
}
