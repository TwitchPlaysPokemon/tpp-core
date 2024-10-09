using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public interface ITransmutationLogRepo
{
    public Task<TransmutationLog> Log(
        string userId,
        Instant timestamp,
        int cost,
        IReadOnlyList<string> inputBadges,
        string outputBadge);
}
