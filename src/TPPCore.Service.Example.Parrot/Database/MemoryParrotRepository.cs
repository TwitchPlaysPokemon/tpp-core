using TPPCore.Database;

namespace TPPCore.Service.Example.Parrot
{
    public class MemoryParrotRepository : ParrotRepository
    {
        private DataProvider _provider;
        public MemoryParrotRepository(DataProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Get the contents of the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override string GetContents(int id)
        {
            return _provider.GetDataFromCommand($"contents {id}");
        }

        /// <summary>
        /// Get the timestamp that the item was created.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override string GetTimestamp(int id)
        {
            return _provider.GetDataFromCommand($"timestamp {id}");
        }

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        public override string GetMaxId()
        {
            return _provider.GetDataFromCommand("maxkey");
        }


        /// <summary>
        /// Remove all items.
        /// </summary>
        public override void Remove()
        {
            _provider.ExecuteCommand("removeall");
        }

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public override void Remove(int id)
        {
            _provider.ExecuteCommand($"remove {id}");
        }

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public override void Insert(string message)
        {
            _provider.ExecuteCommand($"insert {message}");
        }
    }
}
