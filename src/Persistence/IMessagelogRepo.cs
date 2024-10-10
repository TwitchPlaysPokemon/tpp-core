using System.Threading.Tasks;
using NodaTime;
using Model;

namespace Persistence
{
    public interface IMessagelogRepo
    {
        Task<Messagelog> LogChat(string userId, string ircLine, string message, Instant timestamp);
    }
}
