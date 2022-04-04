using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public interface IInputLogRepo
{
    Task<InputLog> LogInput(User user, string message, Instant timestamp);
}
