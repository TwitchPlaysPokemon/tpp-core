using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Service.Common;
using TPPCore.Service.Common.AspNetUtils;

namespace TPPCore.Service.Emotes
{
    public class EmoteHandler
    {
        private ServiceContext context;
        private EmoteApiResponse response;
        private HttpClient httpClient;
        private static readonly string URL = "https://api.twitch.tv/kraken/chat/emoticon_images";
        private readonly string _fileLocation;

        public EmoteHandler(ServiceContext context, string fileLocation)
        {
            this.context = context;
            _fileLocation = fileLocation;
        }

        public async Task GetEmotes(CancellationToken token)
        {
            try
            {
                if (File.Exists(_fileLocation) && new FileInfo(_fileLocation).Length > 0)
                {
                    string serialized = await File.ReadAllTextAsync(_fileLocation);
                    EmoteApiResponse deserialized = JsonConvert.DeserializeObject<EmoteApiResponse>(serialized);
                    response = deserialized;
                }
            } catch
            {
            }

            string clientId = context.ConfigReader.GetCheckedValue<string>("emote", "client_id");
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("accept", "application/vnd.twitchtv.v5+json");
            httpClient.DefaultRequestHeaders.Add("client-id", clientId);
            try
            {
                HttpResponseMessage responseMessage = await httpClient.GetAsync(URL, token);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string JsonString = await responseMessage.Content.ReadAsStringAsync();
                    EmoteApiResponse apiResponse = JsonConvert.DeserializeObject<EmoteApiResponse>(JsonString);
                    if (apiResponse != response)
                    {

                        response = apiResponse;
                        await File.WriteAllTextAsync(_fileLocation, JsonString);
                    }
                }
                else
                {
                    await GetEmotes(token);
                }
            } catch
            {
                return;
            }
        }

        public async Task GetEmoteFromId(HttpContext context)
        {
            string unparsed = (string)context.GetRouteValue("id");
            int.TryParse(unparsed, out int Id);
            EmoteApiResponse.Emote emote = response.emoticons.Where(x => x.id == Id).First();
            EmoteInfo info = new EmoteInfo
            {
                Code = emote.code,
                Id = emote.id,
                ImageUrls = new List<string> { $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/1.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/2.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/3.0" }
            };

            string JsonResponse = JsonConvert.SerializeObject(info);

            await context.RespondStringAsync(JsonResponse);
        }

        public async Task GetEmoteFromCode(HttpContext context)
        {
            string code = (string)context.GetRouteValue("code");
            EmoteApiResponse.Emote emote = response.emoticons.Where(x => Regex.IsMatch(code.ToLower(), x.code.ToLower())).First();
            EmoteInfo info = new EmoteInfo
            {
                Code = emote.code,
                Id = emote.id,
                ImageUrls = new List<string> { $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/1.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/2.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/3.0" }
            };

            string JsonResponse = JsonConvert.SerializeObject(info);

            await context.RespondStringAsync(JsonResponse);
        }

        public async Task EmoteCodeToId(HttpContext context)
        {
            string code = (string)context.GetRouteValue("code");
            EmoteApiResponse.Emote emote = response.emoticons.Where(x => Regex.IsMatch(code.ToLower(), x.code.ToLower())).First();
            int Id = emote.id;

            string JsonResponse = JsonConvert.SerializeObject(Id);

            await context.RespondStringAsync(JsonResponse);
        }

        public async Task EmoteIdToCode(HttpContext context)
        {
            string unparsed = (string)context.GetRouteValue("id");
            int.TryParse(unparsed, out int Id);
            EmoteApiResponse.Emote emote = response.emoticons.Where(x => x.id == Id).First();
            string code = emote.code;

            string JsonRespose = JsonConvert.SerializeObject(code);

            await context.RespondStringAsync(JsonRespose);
        }

        public async Task EmoteIdToUrl(HttpContext context)
        {
            string unparsed = (string)context.GetRouteValue("id");
            int.TryParse(unparsed, out int Id);
            string scale = (string)context.GetRouteValue("scale");
            string URL = $"https://static-cdn.jtvnw.net/emoticons/v1/{Id}/{scale}";

            string JsonResponse = JsonConvert.SerializeObject(URL);

            await context.RespondStringAsync(JsonResponse);
        }

        public async Task FindEmotes(HttpContext context)
        {
            string text = (string)context.GetRouteValue("text");

            IEnumerable<EmoteApiResponse.Emote> matches = response.emoticons.Where(x => x.code.ToLower().Contains(text.ToLower()));
            List<EmoteInfo> prepared = new List<EmoteInfo> { };
            foreach (EmoteApiResponse.Emote emote in matches)
            {
                prepared.Add(new EmoteInfo
                {
                    Code = emote.code,
                    Id = emote.id,
                    ImageUrls = new List<string> { $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/1.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/2.0", $"https://static-cdn.jtvnw.net/emoticons/v1/{emote.id}/3.0" }
                });
            }

            string JsonResponse = JsonConvert.SerializeObject(prepared);

            await context.RespondStringAsync(JsonResponse);
        }
    }
}
