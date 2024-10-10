using System.Threading.Tasks;

namespace Persistence
{
    public interface IKeyValueStore
    {
        public Task<T?> Get<T>(string key);
        public Task Set<T>(string key, T value);
        public Task Delete<T>(string key);
    }
}
