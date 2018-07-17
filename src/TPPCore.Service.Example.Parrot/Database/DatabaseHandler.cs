using System.Threading.Tasks;

namespace TPPCore.Service.Example.Parrot
{
    public class DatabaseHandler
    {
        private IParrotRepository _parrotRepository;
        public DatabaseHandler(IParrotRepository parrotRepository)
        {
            _parrotRepository = parrotRepository;
        }

        public async Task SaveToDatabase(string data)
        {
            await _parrotRepository.Insert(data);
        }

        public async Task<string> GetContents(int id)
        {
            return (await _parrotRepository.GetRecord(id))[1];
        }

        public async Task<int> GetMaxId()
        {
            return await _parrotRepository.GetMaxId();
        }

        public async Task<string> GetTimestamp(int id)
        {
            return (await _parrotRepository.GetRecord(id))[2];
        }

        public async Task ClearDatabase()
        {
            await _parrotRepository.Wipe();
        }

        public async Task RemoveFromDatabase(int id)
        {
            await _parrotRepository.Remove(id);
        }
    }
}
