using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Emotes
{
    public class TwitchEmoteInterface
    {
        private static readonly string URL = "https://api.twitch.tv/kraken/chat/emoticon_images";

        public static List<Tuple<string, string>> ProblematicEmotes = new List<Tuple<string, string>>
        {
            new Tuple<string, string> (":[z|Z||]", ":z"),
            new Tuple<string, string> ("[oO](_|.)[oO]", "o_O"),
            new Tuple<string, string> (":(o|O)", ":o"),
            new Tuple<string, string> (":[\\/]", ":\\"),
            new Tuple<string, string> (":(p|P)", ":p"),
            new Tuple<string, string> (";(p|P)", ";p"),
            new Tuple<string, string> ("#[\\/]", "#/"),
            new Tuple<string, string> (":(?:7|L)", ":7"),
            new Tuple<string, string> (":(S|s)", ":s")
        };

        public async Task<string> GetEmotes(ServiceContext context, HttpClient httpClient, CancellationToken token)
        {
            string clientId = context.ConfigReader.GetCheckedValue<string>("emote", "client_id");
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("accept", "application/vnd.twitchtv.v5+json");
            httpClient.DefaultRequestHeaders.Add("client-id", clientId);
            try
            {
                HttpResponseMessage responseMessage = await httpClient.GetAsync(URL, token);

                if (responseMessage.IsSuccessStatusCode)
                {
                    return await responseMessage.Content.ReadAsStringAsync();
                }
                else
                {
                    return await GetEmotes(context, httpClient, token);
                }
            }
            catch
            {
                return "[]";
            }
        }
    }
}
