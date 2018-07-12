using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System.Threading.Tasks;
using TPPCore.Service.Common.AspNetUtils;

namespace TPPCore.Service.Example.Parrot
{
    public class DatabaseHandler
    {
        private IParrotRepository _parrotRepository;
        public DatabaseHandler(IParrotRepository parrotRepository)
        {
            _parrotRepository = parrotRepository;
        }

        public async Task SaveToDatabase(string serialized)
        {
            string unserialized = JsonConvert.DeserializeObject<string>(serialized);
            await _parrotRepository.Insert(unserialized);
        }

        public async Task<string> GetContents(int id)
        {
            return await _parrotRepository.GetContents(id);
        }

        public async Task<int> GetMaxId()
        {
            return await _parrotRepository.GetMaxId();
        }

        public async Task GetMaxId(HttpContext httpContext)
        {
            string jsondoc = JsonConvert.SerializeObject(await _parrotRepository.GetMaxId());

            await httpContext.RespondStringAsync(jsondoc);
        }

        public async Task GetContents(HttpContext httpContext)
        {
            int.TryParse((string)httpContext.GetRouteValue("id"), out int result);
            string contents = await _parrotRepository.GetContents(result);
            string jsondoc = JsonConvert.SerializeObject(contents);

            await httpContext.RespondStringAsync(jsondoc);
        }

        public async Task<string> GetTimestamp(int id)
        {
            return await _parrotRepository.GetTimestamp(id);
        }

        public async Task ClearDatabase()
        {
            await _parrotRepository.Remove();
        }

        public async Task RemoveFromDatabase(int id)
        {
            await _parrotRepository.Remove(id);
        }
    }
}
