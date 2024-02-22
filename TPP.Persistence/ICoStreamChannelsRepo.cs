using System.Collections.Immutable;
using System.Threading.Tasks;

namespace TPP.Persistence;

public interface ICoStreamChannelsRepo
{
    public Task<bool> IsJoined(string channelName);
    public Task<string?> GetChannelImageUrl(string channelName);
    public Task<IImmutableSet<string>> GetJoinedChannels();
    public Task Add(string channelName, string? profileImageUrl);
    public Task Remove(string channelName);
}
