using System.Threading.Tasks;
using NodaTime;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IMessagelogRepo
    {
        Task<Messagelog> LogChat(string userId, string ircLine, string message, Instant timestamp);
    }
}
