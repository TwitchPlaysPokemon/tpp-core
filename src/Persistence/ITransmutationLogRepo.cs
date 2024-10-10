using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;
using Model;

namespace Persistence;

public interface ITransmutationLogRepo
{
    public Task<TransmutationLog> Log(
        string userId,
        Instant timestamp,
        int cost,
        IReadOnlyList<string> inputBadges,
        string outputBadge);
}
