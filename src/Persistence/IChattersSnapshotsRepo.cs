using System.Collections.Immutable;
using System.Threading.Tasks;
using NodaTime;
using Model;

namespace Persistence;

public interface IChattersSnapshotsRepo
{
    Task<ChattersSnapshot> LogChattersSnapshot(
        IImmutableList<string> chatterNames,
        IImmutableList<string> chatterIds,
        string channel,
        Instant timestamp);
}
