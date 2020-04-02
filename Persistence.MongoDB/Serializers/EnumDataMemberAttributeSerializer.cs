using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Persistence.MongoDB.Serializers
{
    /// <summary>
    /// A serializer for storing enums in the database as a string using the <see cref="DataMemberAttribute.Name"/>
    /// that was given to each enum with a <see cref="DataMemberAttribute"/>.
    /// </summary>
    public class EnumDataMemberAttributeSerializer<T> : SerializerBase<T> where T : struct
    {
        public static readonly EnumDataMemberAttributeSerializer<T> Instance =
            new EnumDataMemberAttributeSerializer<T>();

        private static readonly Dictionary<string, T> DeserLookup;
        private static readonly Dictionary<T, string> SerLookup;

        static EnumDataMemberAttributeSerializer()
        {
            DeserLookup = new Dictionary<string, T>();
            SerLookup = new Dictionary<T, string>();
            foreach (T enumValue in Enum.GetValues(typeof(T)).Cast<T>())
            {
                MemberInfo enumValueMemberInfo = typeof(T).GetMember(enumValue.ToString()!).First();
                var attr = (DataMemberAttribute?) enumValueMemberInfo
                    .GetCustomAttribute(typeof(DataMemberAttribute));
                if (attr == null || !attr.IsNameSetExplicitly || DeserLookup.ContainsKey(attr.Name))
                {
                    throw new ArgumentException(
                        $"enum type {typeof(T)} must give all its values a unique name using a {typeof(DataMemberAttribute)}");
                }
                DeserLookup.Add(attr.Name, enumValue);
                SerLookup.Add(enumValue, attr.Name);
            }
        }

        public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            string rawString = context.Reader.ReadString();
            if (DeserLookup.TryGetValue(rawString, out T result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException(
                    $"Cannot deserialize string {rawString} into enum {typeof(T)}, because " +
                    $"none of the enum values have that name assigned via their {typeof(DataMemberAttribute)}");
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
        {
            context.Writer.WriteString(SerLookup[value]);
        }
    }
}
