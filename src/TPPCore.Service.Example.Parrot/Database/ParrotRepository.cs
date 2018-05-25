namespace TPPCore.Service.Example.Parrot
{
    public abstract class ParrotRepository
    {
        /// <summary>
        /// Get the contents of the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract string GetContents(int id);

        /// <summary>
        /// Get the timestamp that the item was created.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract string GetTimestamp(int id);

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        public abstract string GetMaxId();

        /// <summary>
        /// Remove all items.
        /// </summary>
        public abstract void Remove();

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public abstract void Remove(int id);

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public abstract void Insert(string message);
    }
}
