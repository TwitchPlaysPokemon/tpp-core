using System.Threading.Tasks;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface IMessagequeueRepo
    {
        Task<MessagequeueItem> EnqueueMessage(string ircLine);
    }
}
