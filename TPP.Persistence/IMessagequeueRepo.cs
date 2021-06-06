using System.Threading.Tasks;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IMessagequeueRepo
    {
        Task<MessagequeueItem> EnqueueMessage(string ircLine);
    }
}
