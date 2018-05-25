using TPPCore.Database;

namespace TPPCore.Service.Example.Parrot
{
    public class PostgresqlParrotRepository : ParrotRepository
    {
        private DataProvider _provider;
        public PostgresqlParrotRepository(DataProvider provider)
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
            return _provider.GetDataFromCommand($"SELECT parrot_return_contents({id});");
        }

        /// <summary>
        /// Get the timestamp that the item was created.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override string GetTimestamp(int id)
        {
            return _provider.GetDataFromCommand($"SELECT parrot_return_timestamp({id});");
        }

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        public override string GetMaxId()
        {
            return _provider.GetDataFromCommand("SELECT parrot_return_max_key();");
        }

        /// <summary>
        /// Remove all items.
        /// </summary>
        public override void Remove()
        {
            _provider.ExecuteCommand("SELECT parrot_delete();");
        }

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public override void Remove(int id)
        {
            _provider.ExecuteCommand($"SELECT parrot_delete({id});");
        }

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public override void Insert(string message)
        {
            _provider.ExecuteCommand($"SELECT parrot_insert('{message}');");
        }
    }
}
