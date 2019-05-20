using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Emotes
{
    public class BttvEmoteInterface : IEmoteInterface
    {
        public async Task<string> GetEmotes(ServiceContext context, CancellationToken token)
        {
            HttpClient client = new HttpClient();
            try
            {
                HttpResponseMessage message = await client.GetAsync("https://api.betterttv.net/2/emotes", token);

                if (message.IsSuccessStatusCode)
                {
                    return await message.Content.ReadAsStringAsync();
                }

                return await GetEmotes(context, token);
            }
            catch
            {
                return "{}";
            }
        }
    }
}
