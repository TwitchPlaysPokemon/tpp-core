
using System;
using System.Threading;
using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Persistence;

public interface IIncomingMessagequeueRepo
{
    Task Prune();
    Task ForEachAsync(
        Func<IncomingMessagequeueItem, Task> process,
        CancellationToken cancellationToken);
}
