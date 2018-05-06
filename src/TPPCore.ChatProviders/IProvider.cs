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
        /// Account's unique user ID usually numerical identifier.
        /// </summary>
        string GetUserId();

        /// <summary>
        /// Account's username that doesn't change often.
        /// </summary>
        string GetUsername();
    }

    public interface IProviderThreaded : IProvider
    {
        void Run();
        void SendMessage(string channel, string message);
        void SendPrivateMessage(string user, string message);
        PostRoomList GetRoomList(string channel);
    }

    public interface IProviderAsync : IProvider
    {
        Task Run();
        Task SendMessage(string channel, string message);
        Task SendPrivateMessage(string user, string message);
        Task<PostRoomList> GetRoomList(string channel);
    }
}
