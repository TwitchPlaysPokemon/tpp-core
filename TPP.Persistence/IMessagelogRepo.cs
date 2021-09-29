using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public interface IMessagelogRepo
{
    Task<Messagelog> LogChat(string userId, string ircLine, string message, Instant timestamp);
}
