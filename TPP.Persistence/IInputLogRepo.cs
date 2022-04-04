using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public interface IInputLogRepo
{
    Task<InputLog> LogInput(string userId, string message, Instant timestamp);
}
