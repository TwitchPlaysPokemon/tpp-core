using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Persistence;

public interface IMessagequeueRepo
{
    Task<MessagequeueItem> EnqueueMessage(string ircLine);
}
