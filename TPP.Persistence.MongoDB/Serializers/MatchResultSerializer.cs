using MongoDB.Bson.Serialization.Serializers;

namespace TPP.Persistence.MongoDB.Serializers;

/// For historic reasons match results are stored as 0/1/draw instead of blue/red/draw.
public class MatchResultSerializer : SerializerBase<MatchResult>
{
    public static readonly MatchResultSerializer Instance = new();

    public override MatchResult Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        BsonType bsonType = context.Reader.CurrentBsonType;
        if (bsonType == BsonType.Int32)
        {
            int num = context.Reader.ReadInt32();
            return num switch
            {
                0 => MatchResult.Blue,
                1 => MatchResult.Red,
                _ => throw new ArgumentException($"Cannot deserialize unknown numeric value {num} for match result")
            };
        }
        else if (bsonType == BsonType.String)
        {
            string resultStr = context.Reader.ReadString();
            return resultStr switch
            {
                "blue" => MatchResult.Blue,
                "red" => MatchResult.Red,
                "draw" => MatchResult.Draw,
                _ => throw new ArgumentException($"Cannot deserialize unknown match result string '{resultStr}'")
            };
        }
        else
        {
            throw new ArgumentException($"unexpected bson type '{bsonType}' while deserializing match result");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MatchResult value)
    {
        if (value == MatchResult.Draw)
            context.Writer.WriteString("draw");
        else if (value == MatchResult.Blue)
            context.Writer.WriteInt32(0);
        else if (value == MatchResult.Red)
            context.Writer.WriteInt32(1);
        else
            throw new ArgumentException($"cannot serialize unknown match result '{value}'");
    }
}
