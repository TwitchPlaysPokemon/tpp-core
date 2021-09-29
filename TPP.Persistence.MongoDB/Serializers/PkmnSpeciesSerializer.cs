using MongoDB.Bson.Serialization.Serializers;
using TPP.Common;

namespace TPP.Persistence.MongoDB.Serializers;

/// <summary>
/// Serializer for <see cref="PkmnSpecies"/>.
/// They are saved as strings using their unique <see cref="PkmnSpecies.Id">Id</see>.
/// </summary>
public class PkmnSpeciesSerializer : SerializerBase<PkmnSpecies?>
{
    public static readonly PkmnSpeciesSerializer Instance = new PkmnSpeciesSerializer();

    public override PkmnSpecies? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        else
        {
            return PkmnSpecies.OfId(context.Reader.ReadString());
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PkmnSpecies? value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            context.Writer.WriteString(value.Id);
        }
    }
}
