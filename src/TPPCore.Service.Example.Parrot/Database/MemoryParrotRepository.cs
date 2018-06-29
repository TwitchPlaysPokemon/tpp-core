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

        public void Configure(ServiceContext context)
        {
        }

        /// <summary>
        /// Get the contents of the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetContents(int id)
        {
            return _provider.GetDataFromCommand($"contents {id}");
        }

        /// <summary>
        /// Get the timestamp that the item was created.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetTimestamp(int id)
        {
            return _provider.GetDataFromCommand($"timestamp {id}");
        }

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        public int GetMaxId()
        {
            string key = _provider.GetDataFromCommand("maxkey");
            int.TryParse(key, out int result);
            return result;
        }


        /// <summary>
        /// Remove all items.
        /// </summary>
        public void Remove()
        {
            _provider.ExecuteCommand("removeall");
        }

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public void Remove(int id)
        {
            _provider.ExecuteCommand($"remove {id}");
        }

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public void Insert(string message)
        {
            _provider.ExecuteCommand($"insert {message}");
        }
    }
}
