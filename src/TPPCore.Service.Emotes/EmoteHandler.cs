using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        private readonly ServiceContext context;
        public Dictionary<string, EmoteInfo> _emotesByCode = new Dictionary<string, EmoteInfo> { };
        public Dictionary<string, EmoteInfo> _emotesById = new Dictionary<string, EmoteInfo> { };
        private readonly string _fileLocation;
        private readonly string _bttvLocation;

        public EmoteHandler(ServiceContext context, string fileLocation, string bttvLocation)
        {
            this.context = context;
            _fileLocation = fileLocation;
            _bttvLocation = bttvLocation;
        }

        public async Task GetEmotes(CancellationToken token, bool loadFile)
        {
            if (loadFile)
            {
                try
                {
                    if (File.Exists(_fileLocation) && new FileInfo(_fileLocation).Length > 0)
                    {
                        string serialized = await File.ReadAllTextAsync(_fileLocation, token);
                        TwitchEmoteApiResponse deserialized = JsonConvert.DeserializeObject<TwitchEmoteApiResponse>(serialized);
                        foreach (TwitchEmoteApiResponse.Emote emote in deserialized.emoticons)
                        {
                            emote.code = WebUtility.HtmlDecode(Regex.Unescape(emote.code.Replace("-?", "")));
                            List<Tuple<string, string[]>> tuples = TwitchEmoteInterface.ProblematicEmotes
                                .Where(x => x.Item1 == emote.code).ToList();
                            TwitchEmote info = null;
                            if (tuples.Count > 0)
                            {
                                emote.code = tuples[0].Item2[0];
                                foreach (string code in tuples[0].Item2)
                                {
                                    TwitchEmote info2 = new TwitchEmote(emote.id, code);
                                    if (!_emotesByCode.Keys.Contains(code))
                                        _emotesByCode.Add(code, info2);
                                }
                            }
                            else
                            {
                                info = new TwitchEmote(emote.id, emote.code);
                                if (!_emotesByCode.Keys.Contains(emote.code))
                                    _emotesByCode.Add(emote.code, info);
                            }

                            if (info == null)
                                info = new TwitchEmote(emote.id, emote.code);
                            if (!_emotesById.Keys.Contains(emote.id))
                                _emotesById.Add(emote.id, info);
                        }
                    }
                    if (File.Exists(_bttvLocation) && new FileInfo(_bttvLocation).Length > 0)
                    {
                        string serialized = await File.ReadAllTextAsync(_bttvLocation, token);
                        BttvEmoteApiResponse deserialized = JsonConvert.DeserializeObject<BttvEmoteApiResponse>(serialized);
                        foreach (BttvEmoteApiResponse.Emote emote in deserialized.emotes.Where(x => string.IsNullOrWhiteSpace(x.channel)))
                        {
                            BttvEmote info = new BttvEmote(emote.id, emote.code);
                            if (!_emotesByCode.Keys.Contains(emote.code))
                                _emotesByCode.Add(emote.code, info);

                            if (!_emotesById.Keys.Contains(emote.id))
                                _emotesById.Add(emote.id, info);
                        }
                    }
                }
                catch
                {
                }
            }

            TwitchEmoteInterface emoteInterface = new TwitchEmoteInterface();
            BttvEmoteInterface bttvInterface = new BttvEmoteInterface();
            string JsonString = await emoteInterface.GetEmotes(context, token);
            TwitchEmoteApiResponse apiResponse = JsonConvert.DeserializeObject<TwitchEmoteApiResponse>(JsonString);
            string JsonStringBttv = await bttvInterface.GetEmotes(context, token);
            BttvEmoteApiResponse bttvResponse = JsonConvert.DeserializeObject<BttvEmoteApiResponse>(JsonStringBttv);

            if (apiResponse == null || bttvResponse == null)
                await GetEmotes(token, false);

            if (token.IsCancellationRequested) return;

            bool changed = false;
            foreach (TwitchEmoteApiResponse.Emote emote in apiResponse.emoticons)
            {
                emote.code = WebUtility.HtmlDecode(Regex.Unescape(emote.code.Replace("-?", "")));
                List<Tuple<string, string[]>> tuples =
                    TwitchEmoteInterface.ProblematicEmotes.Where(x => x.Item1 == emote.code).ToList();
                TwitchEmote info = null;
                if (tuples.Count > 0)
                {
                    emote.code = tuples[0].Item2[0];
                    foreach (string code in tuples[0].Item2)
                    {
                        TwitchEmote info2 = new TwitchEmote(emote.id, code);
                        if (_emotesByCode.Keys.Contains(code)) continue;
                        _emotesByCode.Add(code, info2);
                        changed = true;
                    }
                }
                else
                {
                    info = new TwitchEmote(emote.id, emote.code);
                    if (!_emotesByCode.Keys.Contains(emote.code))
                    {
                        _emotesByCode.Add(emote.code, info);
                        changed = true;
                    }
                }
                
                if (info == null)
                    info = new TwitchEmote(emote.id, emote.code);

                if (_emotesById.Keys.Contains(emote.id)) continue;
                _emotesById.Add(emote.id, info);
                changed = true;
            }

            if (changed)
                await File.WriteAllTextAsync(_fileLocation, JsonString, token);

            changed = false;
            foreach (BttvEmoteApiResponse.Emote emote in bttvResponse.emotes.Where(x => string.IsNullOrWhiteSpace(x.channel)))
            {
                BttvEmote info = new BttvEmote(emote.id, emote.code);
                if (!_emotesByCode.Keys.Contains(emote.code))
                {
                    _emotesByCode.Add(emote.code, info);
                    changed = true;
                }

                if (_emotesById.Keys.Contains(emote.id)) continue;
                _emotesById.Add(emote.id, info);
                changed = true;
            }

            if (changed)
                await File.WriteAllTextAsync(_bttvLocation, JsonStringBttv, token);

        }

        public async Task GetEmoteFromId(HttpContext context)
        {
            string Id = Uri.UnescapeDataString((string)context.GetRouteValue("id"));

            _emotesById.TryGetValue(Id, out EmoteInfo info);

            await context.RespondStringAsync(JsonConvert.SerializeObject(info));
        }

        public async Task GetEmoteFromCode(HttpContext context)
        {
            string code = Uri.UnescapeDataString((string)context.GetRouteValue("code"));

            _emotesByCode.TryGetValue(code, out EmoteInfo info);

            await context.RespondStringAsync(JsonConvert.SerializeObject(info));
        }

        private List<EmoteInfo> FindEmotes(string text)
        {
            string[] parts = text.Split(new[] { ' ' });
            List<EmoteInfo> info = new List<EmoteInfo>();
            foreach (string part in parts)
            {
                info.AddRange(_emotesByCode.Values.Where(x => part == x.Code).ToList());
            }

            return info;
        }

        public async Task FindEmotesPost(HttpContext context)
        {
            await context.RespondStringAsync(JsonConvert.SerializeObject(FindEmotes(await context.ReadStringAsync())));
        }

        public async Task FindEmotesGet(HttpContext context)
        {
            await context.RespondStringAsync(JsonConvert.SerializeObject(FindEmotes(Uri.UnescapeDataString((string)context.GetRouteValue("text")))));
        }
    }
}
