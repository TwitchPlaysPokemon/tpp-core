using System.IO;
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

        public void Configure(ServiceContext context)
        {
            string filepath = context.ConfigReader.GetCheckedValue<string>("database", "setup");
            string commands = File.ReadAllText(filepath);
            _provider.ExecuteCommand(commands);
        }

        /// <summary>
        /// Get the contents of the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetContents(int id)
        {
            return _provider.GetDataFromCommand($"SELECT parrot_return_contents({id});");
        }

        /// <summary>
        /// Get the timestamp that the item was created.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetTimestamp(int id)
        {
            return _provider.GetDataFromCommand($"SELECT parrot_return_timestamp({id});");
        }

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        public int GetMaxId()
        {
            string idstring =_provider.GetDataFromCommand("SELECT parrot_return_max_key();");
            int.TryParse(idstring, out int result);
            return result;
        }

        /// <summary>
        /// Remove all items.
        /// </summary>
        public void Remove()
        {
            _provider.ExecuteCommand("SELECT parrot_delete();");
        }

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public void Remove(int id)
        {
            _provider.ExecuteCommand($"SELECT parrot_delete({id});");
        }

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public void Insert(string message)
        {
            _provider.ExecuteCommand($"SELECT parrot_insert('{message}');");
        }
    }
}
