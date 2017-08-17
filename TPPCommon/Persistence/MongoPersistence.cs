using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TPPCommon.Models;

namespace TPPCommon.Persistence
{
    /// <summary>
    /// Implementation of <see cref="IPersistence"/> using MongoDB.
    /// </summary>
    public class MongoPersistence : IPersistence
    {
        private readonly IDictionary<Type, string> _collectionLookup;
        private readonly ISet<string> _usedCollectionNames;
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;

        public MongoPersistence(string host, int port, string database) : this(host, port, database, null, null)
        {
        }

        public MongoPersistence(string host, int port, string database, string username, SecureString password)
        {
            var clientSettings = new MongoClientSettings
            {
                Server = new MongoServerAddress(host, port)
            };
            if (username != null && password != null)
            {
                clientSettings.Credentials = new[] {MongoCredential.CreateCredential(database, username, password)}; 
            }
            password?.Dispose();
            _client = new MongoClient(clientSettings);
            _database = _client.GetDatabase(database);
            _collectionLookup = new Dictionary<Type, string>();
            _usedCollectionNames = new HashSet<string>();
            Init();
        }
        
        private void Init()
        {
            // Associate Models with MongoDB collection names
            RegisterCollection<User>("users");
            
            // Set up MongoDB mappings
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.MapIdMember(m => m.Id);
                cm.MapMember(m => m.ProvidedId).SetElementName("provided_id");
                cm.MapMember(m => m.ProvidedName).SetElementName("provided_name");
                cm.MapMember(m => m.Name).SetElementName("name");
                cm.MapMember(m => m.SimpleName).SetElementName("simple_name");
                cm.MapCreator(m => new User(m.Id, m.ProvidedId, m.Name, m.SimpleName, m.ProvidedName));
            });
        }

        private void RegisterCollection<TModel>(string collectionName) where TModel : Model
        {
            if (_usedCollectionNames.Contains(collectionName))
            {
                throw new ArgumentException("Collection name already in use: " + collectionName);
            }
            _collectionLookup.Add(typeof(TModel), collectionName);
            _usedCollectionNames.Add(collectionName);
        }

        private IMongoCollection<T> GetCollection<T>()
        {
            var type = typeof(T);
            if (!_collectionLookup.ContainsKey(type))
            {
                throw new ArgumentException("No collection is registered for type " + type);
            }
            var collectionName = _collectionLookup[type];
            return _database.GetCollection<T>(collectionName);
        }

        public void Save<TModel>(TModel model) where TModel : Model
        {
            GetCollection<TModel>().InsertOne(model);
        }

        public void ReplaceOne<TModel>(Expression<Func<TModel, bool>> expression, TModel replacement, bool upsert = true) where TModel : Model
        {
            GetCollection<TModel>().ReplaceOne(expression, replacement, new UpdateOptions {IsUpsert = upsert});
        }
        
        public TModel FindOne<TModel>(Expression<Func<TModel, bool>> expression) where TModel : Model
        {
            var result = GetCollection<TModel>().Find(expression);
            return result.Any() ? result.Single() : null;
        }
    }
}