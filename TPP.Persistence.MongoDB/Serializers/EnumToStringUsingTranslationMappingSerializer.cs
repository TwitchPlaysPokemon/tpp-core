using MongoDB.Bson.Serialization.Serializers;

namespace TPP.Persistence.MongoDB.Serializers;

/// <summary>
/// A serializer for storing enums in the database as strings using a provided translation mapping.
/// The recommended usage is to create a subclass for each enum that gets stored in the database.
/// </summary>
public abstract class EnumToStringUsingTranslationMappingSerializer<T> : SerializerBase<T> where T : Enum
{
    private readonly ImmutableDictionary<T, string> _translation;
    private readonly ImmutableDictionary<string, T> _translationBack;

    protected EnumToStringUsingTranslationMappingSerializer(Dictionary<T, string> translation)
    {
        _translation = translation.ToImmutableDictionary();
        foreach (T enumValue in Enum.GetValues(typeof(T)).OfType<T>())
        {
            if (!_translation.ContainsKey(enumValue))
            {
                throw new ArgumentException(
                    $"enum translation must be exhaustive, but '{enumValue}' is missing.");
            }
        }
        var translationBack = new Dictionary<string, T>();
        foreach ((T key, string value) in _translation)
        {
            if (translationBack.ContainsKey(value))
            {
                throw new ArgumentException(
                    $"enum translation values must be unique, but '{value}' was used multiple times");
            }
            translationBack[value] = key;
        }
        _translationBack = translationBack.ToImmutableDictionary();
    }

    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        string valueString = context.Reader.ReadString();
        if (!_translationBack.TryGetValue(valueString, out T? value))
        {
            throw new InvalidOperationException(
                $"encountered unknown enum string value '{valueString}' in db for enum '{typeof(T)}'");
        }
        return value;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        context.Writer.WriteString(_translation[value]);
    }
}
