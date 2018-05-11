using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Client.Common;

namespace TPPCore.Client.Chat
{
    public class ChatClient
    {
        private readonly string Url;
        private string Client;
        private string Channel;
        private readonly RestfulClient HttpClient;
        public ChatClient(string Url, string Client, string Channel, RestfulClient HttpClient)
        {
            this.Url = Url;
            this.Client = Client;
            this.Channel = Channel;
            this.HttpClient = HttpClient;
        }

        public async Task<string> GetUserId()
        {
            string unparsed = await CommonClient.GetAsync(new Uri ($"{Url}client/{CommonClient.Escape(Client)}/user_id"), HttpClient);
            string userId = JsonConvert.DeserializeObject<string>(unparsed);
            return userId;
        }

        public async Task<string> GetUserName()
        {
            string unparsed = await CommonClient.GetAsync(new Uri (($"{Url}client/{CommonClient.Escape(Client)}/username")), HttpClient);
            string userName = JsonConvert.DeserializeObject<string>(unparsed);
            return userName;
        }

        public async Task SendMessage(string Message)
        {
            PostMessage postMessage = new PostMessage { Channel = Channel, ClientName = Client, Message = Message };
            string message = JsonConvert.SerializeObject(postMessage);
            await CommonClient.PostAsync(new Uri($"{Url}chat/{CommonClient.Escape(Client)}/{CommonClient.Escape(Channel)}/send"), message, HttpClient);
        }

        public async Task SendPrivateMessage(string User, string Message)
        {
            PostPrivateMessage postPrivateMessage = new PostPrivateMessage { User = User, ClientName = Client, Message = Message };
            string message = JsonConvert.SerializeObject(postPrivateMessage);
            await CommonClient.PostAsync(new Uri($"{Url}private_chat/{CommonClient.Escape(Client)}/{CommonClient.Escape(User)}/send"), message, HttpClient);
        }

        public async Task TimeoutUser(string User, string Reason = "", int Duration = 1)
        {
            PostTimeout postTimeout = new PostTimeout { User = User, Channel = Channel, ClientName = Client, Duration = Duration, Reason = Reason };
            string message = JsonConvert.SerializeObject(postTimeout);
            await CommonClient.PostAsync(new Uri($"{Url}chat/{CommonClient.Escape(Client)}/{CommonClient.Escape(Channel)}/timeout"), message, HttpClient);
        }

        public async Task BanUser(string User, string Reason = "")
        {
            PostBan postBan = new PostBan { User = User, Channel = Channel, ClientName = Client, Reason = Reason };
            string message = JsonConvert.SerializeObject(postBan);
            await CommonClient.PostAsync(new Uri($"{Url}chat/{CommonClient.Escape(Client)}/{CommonClient.Escape(Channel)}/ban"), message, HttpClient);
        }

        public async Task<IList<ChatUser>> GetRoomList()
        {
            string unparsed = await CommonClient.GetAsync(new Uri($"{Url}chat/{CommonClient.Escape(Client)}/{CommonClient.Escape(Channel)}/room_list"), HttpClient);
            IList<ChatUser> roomList = JsonConvert.DeserializeObject<IList<ChatUser>>(unparsed);
            return roomList;
        }
    }
}
