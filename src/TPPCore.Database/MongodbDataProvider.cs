using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TPPCore.Database
{
    public class MongodbDataProvider : IDataProvider
    {
        private MongoClient Client;
        private IMongoDatabase Db;

        public MongodbDataProvider(string Database, string Host, string ApplicationName, string Username, string Password, int Port)
        {
            Client = new MongoClient(new MongoClientSettings
            {
                ApplicationName = ApplicationName,
                Server = new MongoServerAddress(Host, Port),
                Credential = Database == null || Username == null || Password == null
                    ? null
                    : MongoCredential.CreateCredential(Database, Username, Password),
                UseSsl = true
            });
            Db = Client.GetDatabase(Database);
        }

        /// <summary>
        /// Execute a non-returning command.
        /// </summary>
        /// <param name="jsonDoc">The json document for adding or replacing</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task ExecuteCommand(string jsonDoc, IDbParameter[] parameters)
        {
            //in this case it doesn't make sense to have multiple parameters, so just take the first
            MongodbParameter parameter = (MongodbParameter) parameters.First();
            IMongoCollection<BsonDocument> collection = Db.GetCollection<BsonDocument>(parameter.collection);
            switch (parameter.operation)
            {
                case MongoOperation.Insert:
                    await collection.InsertOneAsync(BsonDocument.Parse(jsonDoc));
                    break;
                case MongoOperation.Update:
                    UpdateResult updateResult = await collection.UpdateOneAsync(x => x["_id"] == parameter.id,
                        new JsonUpdateDefinition<BsonDocument>(jsonDoc));
                    if (!updateResult.IsAcknowledged)
                        throw new ArgumentException("The document with the given ID doesn't exist, use insert instead");
                    break;
                case MongoOperation.Replace:
                    ReplaceOneResult replaceResult = await collection.ReplaceOneAsync(x => x["_id"] == parameter.id, BsonDocument.Parse(jsonDoc));
                    if (!replaceResult.IsAcknowledged)
                        throw new ArgumentException("The document with the given ID doesn't exist, use insert instead");
                    break;
                case MongoOperation.Delete:
                    DeleteResult deleteResult = await collection.DeleteOneAsync(x => x["_id"] == parameter.id);
                    if (!deleteResult.IsAcknowledged)
                        throw new ArgumentException("The document with the given ID doesn't exist, you cannot delete nothing");
                    break;
            }
        }

        public async Task<object[]> GetDataFromCommand(string command, IDbParameter[] parameters)
        {
            MongodbParameter[] mongoParameters = (MongodbParameter[]) parameters;
            List<string> documents = new List<string>();
            foreach (MongodbParameter mongoParam in mongoParameters)
            {
                IMongoCollection<BsonDocument> result = Db.GetCollection<BsonDocument>(mongoParam.collection);
                documents.AddRange((await result.FindAsync(x => x["_id"] == mongoParam.id)).ToList().Select(x => x.ToJson()));
            }

            return documents.Select(x => (object)x).ToArray();
        }
    }
}
