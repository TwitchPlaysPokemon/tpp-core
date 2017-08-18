using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security;
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
            // Get all model subclasses to read their respective table attributes.
            IEnumerable<Type> modelTypes = typeof(Model).GetTypeInfo().Assembly.GetTypes()
                .Where(type => typeof(Model).GetTypeInfo().IsAssignableFrom(type) && type != typeof(Model));
            foreach (var modelType in modelTypes)
            {
                var attribute = modelType.GetTypeInfo().GetCustomAttribute<TableAttribute>();
                if (attribute == null)
                {
                    // TODO what exception should be thrown? ArgumentException sounds wrong
                    throw new ArgumentException($"Subclasses of {typeof(Model)} must have the {typeof(TableAttribute)} attribute.");
                }
                var collectionName = attribute.Table;
                if (_usedCollectionNames.Contains(collectionName))
                {
                    throw new ArgumentException("Collection name already in use: " + collectionName);
                }
                _collectionLookup.Add(modelType, collectionName);
                _usedCollectionNames.Add(collectionName);
            }
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