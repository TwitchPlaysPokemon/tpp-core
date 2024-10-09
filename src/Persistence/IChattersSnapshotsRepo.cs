using System.Collections.Immutable;
using System.Threading.Tasks;
using NodaTime;
using TPP.Model;

namespace TPP.Persistence;

public interface IChattersSnapshotsRepo
{
    Task<ChattersSnapshot> LogChattersSnapshot(
        IImmutableList<string> chatterNames,
        IImmutableList<string> chatterIds,
        string channel,
        Instant timestamp);
}
