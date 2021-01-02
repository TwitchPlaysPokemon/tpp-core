using System.Threading.Tasks;
using NodaTime;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface IMessagelogRepo
    {
        Task<Messagelog> LogChat(string userId, string ircLine, string message, Instant timestamp);
    }
}
