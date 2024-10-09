using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Persistence;

public interface IOutgoingMessagequeueRepo
{
    Task<OutgoingMessagequeueItem> EnqueueMessage(string ircLine);
}
