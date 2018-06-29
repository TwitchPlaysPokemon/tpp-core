using TPPCore.Service.Common;

namespace TPPCore.Service.Example.Parrot
{
    public interface IParrotRepository
    {
        /// <summary>
        /// Set up the database.
        /// </summary>
        /// <param name="context"></param>
        void Configure(ServiceContext context);

        /// <summary>
        /// Get the contents of the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        string GetContents(int id);

        /// <summary>
        /// Get the timestamp that the item was created.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        string GetTimestamp(int id);

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        int GetMaxId();

        /// <summary>
        /// Remove all items.
        /// </summary>
        void Remove();

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        void Remove(int id);

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        void Insert(string message);
    }
}
