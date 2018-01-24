using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Chat
{
    public interface IProvider
    {
        string Name { get; }

        void Configure(ProviderContext providerContext);
        void Shutdown();

        string GetUserId();
    }

    public interface IProviderThreaded : IProvider
    {
        void Run();
        void SendMessage(string channel, string message);
        void SendPrivateMessage(string user, string message);
        IList<ChatUser> GetRoomList(string channel);
    }

    public interface IProviderAsync : IProvider
    {
        Task Run();
        Task SendMessage(string channel, string message);
        Task SendPrivateMessage(string user, string message);
        Task<IList<ChatUser>> GetRoomList(string channel);
    }
}
