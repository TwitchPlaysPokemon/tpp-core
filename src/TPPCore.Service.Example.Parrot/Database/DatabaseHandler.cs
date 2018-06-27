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

        public void SaveToDatabase(string serialized)
        {
            string unserialized = JsonConvert.DeserializeObject<string>(serialized);
            _parrotRepository.Insert(unserialized);
        }

        public string GetContents(int id)
        {
            return _parrotRepository.GetContents(id);
        }

        public int GetMaxId()
        {
            return _parrotRepository.GetMaxId();
        }

        public async Task GetMaxId(HttpContext httpContext)
        {
            string jsondoc = JsonConvert.SerializeObject(_parrotRepository.GetMaxId());

            await httpContext.RespondStringAsync(jsondoc);
        }

        public async Task GetContents(HttpContext httpContext)
        {
            int.TryParse((string)httpContext.GetRouteValue("id"), out int result);
            string contents = _parrotRepository.GetContents(result);
            string jsondoc = JsonConvert.SerializeObject(contents);

            await httpContext.RespondStringAsync(jsondoc);
        }

        public string GetTimestamp(int id)
        {
            return _parrotRepository.GetTimestamp(id);
        }

        public void ClearDatabase()
        {
            _parrotRepository.Remove();
        }

        public void RemoveFromDatabase(int id)
        {
            _parrotRepository.Remove(id);
        }
    }
}
