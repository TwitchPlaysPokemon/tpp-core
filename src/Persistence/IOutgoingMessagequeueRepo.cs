using System.Threading.Tasks;
using Model;

namespace Persistence;

public interface IOutgoingMessagequeueRepo
{
    Task<OutgoingMessagequeueItem> EnqueueMessage(string ircLine);
}
