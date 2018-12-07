using System.IO;
using System.Threading.Tasks;
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
            string filepath = context.ConfigReader.GetCheckedValue<string, ParrotConfig>("database", "setup");
            string commands = await File.ReadAllTextAsync(filepath);
            await _provider.ExecuteCommand(commands);
        }

        /// <summary>
        /// Get the contents of the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<string> GetContents(int id)
        {
            return await _provider.GetDataFromCommand($"SELECT parrot_return_contents({id});");
        }

        /// <summary>
        /// Get the timestamp that the item was created.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<string> GetTimestamp(int id)
        {
            return await _provider.GetDataFromCommand($"SELECT parrot_return_timestamp({id});");
        }

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetMaxId()
        {
            string idstring = await _provider.GetDataFromCommand("SELECT parrot_return_max_key();");
            int.TryParse(idstring, out int result);
            return result;
        }

        /// <summary>
        /// Remove all items.
        /// </summary>
        public async Task Remove()
        {
            await _provider.ExecuteCommand("SELECT parrot_delete();");
        }

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public async Task Remove(int id)
        {
            await _provider.ExecuteCommand($"SELECT parrot_delete({id});");
        }

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public async Task Insert(string message)
        {
            await _provider.ExecuteCommand($"SELECT parrot_insert('{message}');");
        }
    }
}
