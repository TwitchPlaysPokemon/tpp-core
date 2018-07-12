using System.Threading.Tasks;
using TPPCore.Database;
using TPPCore.Service.Common;

namespace TPPCore.Service.Example.Parrot
{
    public class MemoryParrotRepository : IParrotRepository
    {
        private IDataProvider _provider;
        public MemoryParrotRepository(IDataProvider provider)
        {
            _provider = provider;
        }
#pragma warning disable 1998
        /// <summary>
        /// Set up the database.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Configure(ServiceContext context)
        {
        }
#pragma warning restore 1998
        /// <summary>
        /// Get the contents of the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<string> GetContents(int id)
        {
            return await _provider.GetDataFromCommand($"contents {id}");
        }

        /// <summary>
        /// Get the timestamp that the item was created.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<string> GetTimestamp(int id)
        {
            return await _provider.GetDataFromCommand($"timestamp {id}");
        }

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetMaxId()
        {
            string key = await _provider.GetDataFromCommand("maxkey");
            int.TryParse(key, out int result);
            return result;
        }


        /// <summary>
        /// Remove all items.
        /// </summary>
        public async Task Remove()
        {
            await _provider.ExecuteCommand("removeall");
        }

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public async Task Remove(int id)
        {
            await _provider.ExecuteCommand($"remove {id}");
        }

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public async Task Insert(string message)
        {
            await _provider.ExecuteCommand($"insert {message}");
        }
    }
}
