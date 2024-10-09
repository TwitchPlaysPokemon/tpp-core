using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Core.Moderation
{
    /// performs moderator actions
    public interface IExecutor
    {
        public Task DeleteMessage(string messageId);
        public Task Timeout(User user, string? message, Duration duration);
        public Task Ban(User user, string? message);
        public Task Unban(User user, string? message);
    }
}
