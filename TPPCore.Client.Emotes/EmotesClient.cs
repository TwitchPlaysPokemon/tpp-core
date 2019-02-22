using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Client.Common;

namespace TPPCore.Client.Emotes
{
    public class EmotesClient
    {
        private readonly string Url;
        private readonly RestfulClient HttpClient;

        public EmotesClient(string Url, RestfulClient HttpClient)
        {
            this.Url = Url;
            this.HttpClient = HttpClient;
        }

        public async Task<EmoteInfo> GetEmoteFromId(int id)
        {
            string unparsed = await CommonClient.GetAsync(new Uri($"{Url}emote/fromid/{id}"), HttpClient);
            EmoteInfo parsed = JsonConvert.DeserializeObject<EmoteInfo>(unparsed);
            return parsed;
        }

        public async Task<EmoteInfo> GetEmoteFromCode(string code)
        {
            string unparsed = await CommonClient.GetAsync(new Uri($"{Url}emote/fromcode/{code}"), HttpClient);
            EmoteInfo parsed = JsonConvert.DeserializeObject<EmoteInfo>(unparsed);
            return parsed;
        }

        public async Task<IList<EmoteInfo>> FindIn(string toSearch)
        {
            string unparsed =
                await CommonClient.GetAsync(new Uri($"{Url}emotes/findin/{CommonClient.Escape(toSearch)}"), HttpClient);
            IList<EmoteInfo> parsed = JsonConvert.DeserializeObject<IList<EmoteInfo>>(unparsed);
            return parsed;
        }
    }
}
