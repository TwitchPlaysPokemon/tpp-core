using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Emotes
{
    public interface IEmoteInterface
    {
        Task<string> GetEmotes(ServiceContext context, CancellationToken token);
    }
}
