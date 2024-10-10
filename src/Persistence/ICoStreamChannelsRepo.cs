using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Persistence;

public interface ICoStreamChannelsRepo
{
    public Task<bool> IsJoined(string channelId);
    public Task<string?> GetChannelImageUrl(string channelId);
    public Task<IImmutableSet<string>> GetJoinedChannels();
    public Task Add(string channelId, string? profileImageUrl);
    public Task Remove(string channelId);
}
