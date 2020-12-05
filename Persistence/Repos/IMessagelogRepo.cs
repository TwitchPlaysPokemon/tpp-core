using System.Threading.Tasks;
using NodaTime;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface IMessagelogRepo
    {
        Task<Messagelog> LogChat(User user, string ircLine, string message, Instant timestamp);
    }
}
