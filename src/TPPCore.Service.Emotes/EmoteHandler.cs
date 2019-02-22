﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        private readonly ServiceContext context;
        public Dictionary<string, EmoteInfo> _emotesByCode = new Dictionary<string, EmoteInfo> { };
        public Dictionary<int, EmoteInfo> _emotesById = new Dictionary<int, EmoteInfo> { };
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string _fileLocation;

        public EmoteHandler(ServiceContext context, string fileLocation)
        {
            this.context = context;
            _fileLocation = fileLocation;
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
                        EmoteApiResponse deserialized = JsonConvert.DeserializeObject<EmoteApiResponse>(serialized);
                        foreach (EmoteApiResponse.Emote emote in deserialized.emoticons)
                        {
                            emote.code = WebUtility.HtmlDecode(Regex.Unescape(emote.code.Replace("-?", "")));
                            List<Tuple<string, string[]>> tuples = TwitchEmoteInterface.ProblematicEmotes
                                .Where(x => x.Item1 == emote.code).ToList();
                            if (tuples.Count > 0)
                            {
                                emote.code = tuples[0].Item2[0];
                                foreach (string code in tuples[0].Item2)
                                {
                                    TwitchEmote info = new TwitchEmote(emote.id, code);
                                    if (!_emotesByCode.Keys.Contains(code))
                                        _emotesByCode.Add(code, info);
                                }
                            }
                            else
                            {
                                TwitchEmote info = new TwitchEmote(emote.id, emote.code);
                                if (!_emotesByCode.Keys.Contains(emote.code))
                                    _emotesByCode.Add(emote.code, info);
                            }

                            TwitchEmote info2 = new TwitchEmote(emote.id, emote.code);
                            if (!_emotesById.Keys.Contains(emote.id))
                                _emotesById.Add(emote.id, info2);
                        }
                    }
                }
                catch
                {
                }
            }

            TwitchEmoteInterface emoteInterface = new TwitchEmoteInterface();
            string JsonString = await emoteInterface.GetEmotes(context, httpClient, token);
            EmoteApiResponse apiResponse = JsonConvert.DeserializeObject<EmoteApiResponse>(JsonString);

            if (token.IsCancellationRequested) return;

            bool changed = false;
            foreach (EmoteApiResponse.Emote emote in apiResponse.emoticons)
            {
                emote.code = WebUtility.HtmlDecode(Regex.Unescape(emote.code.Replace("-?", "")));
                List<Tuple<string, string[]>> tuples =
                    TwitchEmoteInterface.ProblematicEmotes.Where(x => x.Item1 == emote.code).ToList();
                if (tuples.Count > 0)
                {
                    emote.code = tuples[0].Item2[0];
                    foreach (string code in tuples[0].Item2)
                    {
                        TwitchEmote info = new TwitchEmote(emote.id, code);
                        if (!_emotesByCode.Keys.Contains(code))
                        {
                            _emotesByCode.Add(code, info);
                            changed = true;
                        }
                    }
                }
                else
                {
                    TwitchEmote info = new TwitchEmote(emote.id, emote.code);
                    if (!_emotesByCode.Keys.Contains(emote.code))
                    {
                        _emotesByCode.Add(emote.code, info);
                        changed = true;
                    }
                }

                TwitchEmote info2 = new TwitchEmote(emote.id, emote.code);

                if (!_emotesById.Keys.Contains(emote.id))
                {
                    _emotesById.Add(emote.id, info2);
                    changed = true;
                }
            }

            if (changed)
                await File.WriteAllTextAsync(_fileLocation, JsonString, token);
        }

        public async Task GetEmoteFromId(HttpContext context)
        {
            string unparsed = Uri.UnescapeDataString((string)context.GetRouteValue("id"));
            int.TryParse(unparsed, out int Id);

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
            List<EmoteInfo> info = new List<EmoteInfo> { };
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
