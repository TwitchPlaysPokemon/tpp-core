using System;
using System.IO;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Database;
using TPPCore.Service.Common;

namespace TPPCore.Service.Example.Parrot
{
    public class PostgresqlParrotRepository : IParrotRepository
    {
        private IDataProvider _provider;
        public PostgresqlParrotRepository(IDataProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Set up the database.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Configure(ServiceContext context)
        {
            string filepath = context.ConfigReader.GetCheckedValue<string>("database", "setup");
            string commands = await File.ReadAllTextAsync(filepath);
            await _provider.ExecuteCommand(commands, new PostgresqlParameter[] { });
        }

        /// <summary>
        /// Get the contents of the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ParrotRecord> GetRecord(int id)
        {
            PostgresqlParameter[] parameters = new[] { new PostgresqlParameter()
            {
                parameterName = "id",
                type = NpgsqlTypes.NpgsqlDbType.Integer,
                value = id
            }};
            object[] data = await _provider.GetDataFromCommand($"SELECT * FROM parrot WHERE ID = @id;", parameters);
            return new ParrotRecord()
            {
                id = (int)data[0],
                contents = (string)data[1],
                timestamp = (DateTime)data[2]
            };
        }

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetMaxId()
        {
            int id = (int)(await _provider.GetDataFromCommand("SELECT CAST(ID as INT) FROM parrot ORDER BY ID DESC LIMIT 1;", new PostgresqlParameter[] { }))[0];
            return id;
        }

        /// <summary>
        /// Remove all items.
        /// </summary>
        public async Task Wipe()
        {
            await _provider.ExecuteCommand("DELETE FROM parrot;", new PostgresqlParameter[] { });
        }

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public async Task Remove(int id)
        {
            PostgresqlParameter[] parameters = new[] { new PostgresqlParameter()
            {
                parameterName = "id",
                type = NpgsqlTypes.NpgsqlDbType.Integer,
                value = id
            }};
            await _provider.ExecuteCommand($"DELETE FROM parrot WHERE ID = @id;", parameters);
        }

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public async Task Insert(string message)
        {
            PostgresqlParameter[] parameters = new[] { new PostgresqlParameter()
            {
                parameterName = "message",
                type = NpgsqlTypes.NpgsqlDbType.Text,
                value = message
            }};
            await _provider.ExecuteCommand("INSERT INTO parrot(contents) VALUES(@message);", parameters);
        }
    }
}
