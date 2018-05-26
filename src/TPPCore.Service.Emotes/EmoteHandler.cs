using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        private Dictionary<string, EmoteInfo> _emotesByCode = new Dictionary<string, EmoteInfo> { };
        private Dictionary<int, EmoteInfo> _emotesById = new Dictionary<int, EmoteInfo> { };
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
                    foreach (EmoteApiResponse.Emote emote in deserialized.emoticons)
                    {
                        EmoteInfo info = new EmoteInfo
                        {
                            Code = emote.code,
                            Id = emote.id,
                            ImageUrls = new List<string> { EmoteIdToUrl(emote.id, "1.0"), EmoteIdToUrl(emote.id, "2.0"), EmoteIdToUrl(emote.id, "3.0") }
                        };
                        if (!_emotesByCode.Keys.Contains(emote.code))
                            _emotesByCode.Add(emote.code, info);

                        if (!_emotesById.Keys.Contains(emote.id))
                            _emotesById.Add(emote.id, info);
                    }
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
                    bool changed = false;
                    foreach (EmoteApiResponse.Emote emote in apiResponse.emoticons)
                    {
                        EmoteInfo info = new EmoteInfo
                        {
                            Code = emote.code,
                            Id = emote.id,
                            ImageUrls = new List<string> { EmoteIdToUrl(emote.id, "1.0"), EmoteIdToUrl(emote.id, "2.0"), EmoteIdToUrl(emote.id, "3.0") }
                        };
                        if (!_emotesByCode.Keys.Contains(emote.code))
                        {
                            _emotesByCode.Add(emote.code, info);
                            changed = true;
                        }

                        if (!_emotesById.Keys.Contains(emote.id))
                        {
                            _emotesById.Add(emote.id, info);
                            changed = true;
                        }
                    }

                    if (changed)
                        await File.WriteAllTextAsync(_fileLocation, JsonString);
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

            _emotesById.TryGetValue(Id, out EmoteInfo info);

            string JsonResponse = JsonConvert.SerializeObject(info);

            await context.RespondStringAsync(JsonResponse);
        }

        public async Task GetEmoteFromCode(HttpContext context)
        {
            string code = (string)context.GetRouteValue("code");

            _emotesByCode.TryGetValue(code, out EmoteInfo info);

            string JsonResponse = JsonConvert.SerializeObject(info);

            await context.RespondStringAsync(JsonResponse);
        }

        public async Task EmoteCodeToId(HttpContext context)
        {
            string code = (string)context.GetRouteValue("code");

            int? id = _emotesByCode[code]?.Id;

            string JsonResponse = JsonConvert.SerializeObject(id);

            await context.RespondStringAsync(JsonResponse);
        }

        public async Task EmoteIdToCode(HttpContext context)
        {
            string unparsed = (string)context.GetRouteValue("id");
            int.TryParse(unparsed, out int Id);

            string code = _emotesById[Id]?.Code;

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

        public string EmoteIdToUrl(int id, string scale)
        {
            return $"https://static-cdn.jtvnw.net/emoticons/v1/{id}/{scale}";
        }

        public async Task FindEmotes(HttpContext context)
        {
            string text = (string)context.GetRouteValue("text");

            List<EmoteInfo> info = _emotesByCode.Values.Where(x => x.Code.Contains(text)).ToList();

            string JsonResponse = JsonConvert.SerializeObject(info);

            await context.RespondStringAsync(JsonResponse);
        }
    }
}
