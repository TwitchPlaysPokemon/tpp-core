using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Emotes
{
    internal class TwitchEmoteInterface
    {
        private const string URL = "https://api.twitch.tv/kraken/chat/emoticon_images";

        public static List<Tuple<string, string[]>> ProblematicEmotes = new List<Tuple<string, string[]>>
        {
            new Tuple<string, string[]> (":)", new[] {":)", ":-)"}),
            new Tuple<string, string[]> (":(", new[] {":(", ":-("}),
            new Tuple<string, string[]> (":D", new[] {":D", ":-D"}),
            new Tuple<string, string[]> ("B)", new[] {"B)", "B-)"}),
            new Tuple<string, string[]> (";)", new[] {";)", ";-)"}),
            new Tuple<string, string[]> ("R)", new[] {"R)", "R-)"}),
            new Tuple<string, string[]> (":[z|Z||]", new[] {":z", ":Z", ":|", ":-z", ":-Z", ":-|"}),
            new Tuple<string, string[]> ("[oO](_|.)[oO]", new[] {"o_O", "o_o", "O_o", "O_O", "o.o", "O.o", "o.O", "O.O"}),
            new Tuple<string, string[]> (":(o|O)", new[] {":o", ":O", ":-o", ":-O"}),
            new Tuple<string, string[]> (":[\\/]", new[] {":\\", ":/", ":-\\", ":-/"}),
            new Tuple<string, string[]> (":(p|P)", new[] {":p", ":P", ":-p", ":-P"}),
            new Tuple<string, string[]> (";(p|P)", new[] {";p", ";P", ";-p", ";-P"}),
            new Tuple<string, string[]> ("#[\\/]", new[] {"#\\", "#/", "#-\\", "#-/"}),
            new Tuple<string, string[]> (":(?:7|L)", new[] {":7", ":L", ":-7", ":-L"}),
            new Tuple<string, string[]> (":(S|s)", new [] {":s", ":S", ":-s", ":-S"})
        };

        public async Task<string> GetEmotes(ServiceContext context, HttpClient httpClient, CancellationToken token)
        {
            string clientId = context.ConfigReader.GetCheckedValue<string, EmotesConfig>("emote", "client_id");
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
                return "{}";
            }
        }
    }
}
