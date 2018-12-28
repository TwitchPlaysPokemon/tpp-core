using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Service.Common;

namespace TPPCore.Service.Example.Parrot
{
    public interface IParrotRepository
    {
        /// <summary>
        /// Set up the database.
        /// </summary>
        /// <param name="context"></param>
        Task Configure(ServiceContext context);

        /// <summary>
        /// Get the record with the corresponding id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ParrotRecord> GetRecord(int id);

        /// <summary>
        /// Get the highest ID that's currently in the database.
        /// </summary>
        /// <returns></returns>
        Task<int> GetMaxId();

        /// <summary>
        /// Remove all items.
        /// </summary>
        Task Wipe();

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        Task Remove(int id);

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        Task Insert(string message);
    }
}
