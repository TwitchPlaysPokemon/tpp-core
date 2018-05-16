using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;

namespace TPPCore.ChatProviders
{
    /// <summary>
    /// Connects to a website or server's chat endpoint.
    /// </summary>
    public interface IProvider
    {
        /// <summary>
        /// Name used to reference and access the provider.
        /// </summary>
        /// <remarks>
        /// This is the friendly name configured for example,
        /// "twitchWelcomeBot" or "ircVoiceBot".
        /// </remarks>
        string ClientName { get; }

        /// <summary>
        /// Name of the website or server's endpoint such as IRC or Twitch.
        /// </summary>
        string ProviderName { get; }

        void Configure(string clientName, ProviderContext providerContext);
        void Shutdown();

        /// <summary>
        /// Account's username that doesn't change often.
        /// </summary>
        string GetUsername();
    }

    public interface IProviderThreaded : IProvider
    {
        void Run();
        /// <summary>
        /// Account's unique user ID usually numerical identifier.
        /// </summary>
        string GetUserId();
        void SendMessage(string channel, string message);
        void SendPrivateMessage(string user, string message);
        void TimeoutUser(ChatUser user, string reason, int duration, string channel);
        void BanUser(ChatUser user, string reason, string channel);
        IList<ChatUser> GetRoomList(string channel);
    }

    public interface IProviderAsync : IProvider
    {
        Task Run();
        /// <summary>
        /// Account's unique user ID usually numerical identifier.
        /// </summary>
        Task<string> GetUserId();
        Task SendMessage(string channel, string message);
        Task SendPrivateMessage(string user, string message);
        Task TimeoutUser(ChatUser user, string reason, int duration, string channel);
        Task BanUser(ChatUser user, string reason, string channel);
        Task<IList<ChatUser>> GetRoomList(string channel);
    }
}
