namespace TPPCore.ChatProviders.DataModels
{
    public class EmoteInfo
    {
        /// <summary>
        /// The name of the emote.
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// The ID of the emote
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The URLs of the emote
        /// </summary>
        public virtual string[] ImageUrls { get; set; }
    }
}
