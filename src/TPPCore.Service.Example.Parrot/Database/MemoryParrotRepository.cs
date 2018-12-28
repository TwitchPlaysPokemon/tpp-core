using System;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
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
        /// Get the record with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ParrotRecord> GetRecord(int id)
        {
            object[] data = await _provider.GetDataFromCommand($"record {id}", null);
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
            try
            {
                int key = (int)(await _provider.GetDataFromCommand("maxkey", null))[0];
                return key;
            }
            catch
            {
                return 0;
            }
        }


        /// <summary>
        /// Remove all items.
        /// </summary>
        public async Task Wipe()
        {
            await _provider.ExecuteCommand("removeall", null);
        }

        /// <summary>
        /// Remove the item with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        public async Task Remove(int id)
        {
            await _provider.ExecuteCommand($"remove {id}", null);
        }

        /// <summary>
        /// Insert an item into the database.
        /// </summary>
        /// <param name="message"></param>
        public async Task Insert(string message)
        {
            await _provider.ExecuteCommand($"insert {message}", null);
        }
    }
}
