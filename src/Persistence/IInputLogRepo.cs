using System.Threading.Tasks;
using NodaTime;
using Model;

namespace Persistence;

public interface IInputLogRepo
{
    Task<InputLog> LogInput(string userId, string message, Instant timestamp);
}
