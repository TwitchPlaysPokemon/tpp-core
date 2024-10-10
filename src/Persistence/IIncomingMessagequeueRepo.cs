
using System;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using Model;

namespace Persistence;

public interface IIncomingMessagequeueRepo
{
    Task Prune(Instant olderThan);
    Task ForEachAsync(
        Func<IncomingMessagequeueItem, Task> process,
        CancellationToken cancellationToken);
}
