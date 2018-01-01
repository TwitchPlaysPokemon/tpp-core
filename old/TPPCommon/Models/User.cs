using MongoDB.Bson.Serialization.Attributes;
using TPPCommon.Persistence;

namespace TPPCommon.Models
{
    /// <summary>
    /// model for user objects.
    /// </summary>
    [Table("users")]
    public class User : Model
    {
        /// <summary>
        /// unique id of the user.
        /// </summary>
        [BsonId]
        public readonly string Id;
        
        /// <summary>
        /// user id that the (chat) service, which this user originates from, provided.
        /// </summary>
        [BsonElement("provided_id")]
        public readonly string ProvidedId;
        
        /// <summary>
        /// name of the (chat) service this user originates from.
        /// </summary>
        [BsonElement("provided_name")]
        public readonly string ProvidedName;
        
        /// <summary>
        /// name of the user. this is how he is being displayed.
        /// </summary>
        [BsonElement("name")]
        public readonly string Name;
        
        /// <summary>
        /// simple name of this user. usually maps to lowercase-variations from irc. only contains ASCII.
        /// </summary>
        [BsonElement("simple_name")]
        public readonly string SimpleName;

        [BsonConstructor]
        public User(string id, string providedId, string name, string simpleName, string providedName)
        {
            Id = id;
            ProvidedId = providedId;
            Name = name;
            SimpleName = simpleName;
            ProvidedName = providedName;
        }
    }
}