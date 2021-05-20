using System.Threading.Tasks;
using NodaTime;
using TPP.Persistence.Models;

namespace TPP.Core.Moderation
{
    /// performs moderator actions
    public interface IExecutor
    {
        public Task DeleteMessage(string messageId);
        public Task Timeout(User user, string? message, Duration duration);
    }
}
