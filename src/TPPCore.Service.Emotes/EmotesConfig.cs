namespace TPPCore.Service.Emotes
{
    public class EmotesConfig
    {
        public EmoteConfig emote;

        public class EmoteConfig
        {
            /// <summary>
            /// The location of the json cache.
            /// </summary>
            public string cache_location;
            /// <summary>
            /// The client id for the twitch api.
            /// </summary>
            public string client_id;
        }
    }
}
