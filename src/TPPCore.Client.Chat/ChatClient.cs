using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.ChatProviders.DataModels;
using TPPCore.Client.Common;

namespace TPPCore.Client.Chat
{
    public class ChatClient
    {
        private string Url;
        private string Client;
        private RestfulClient HttpClient;
        public ChatClient(string Url, string Client, RestfulClient HttpClient)
        {
            this.Url = Url;
            this.Client = Client;
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

        public async Task SendMessage(string Channel, string Message)
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

        public async Task<IList<ChatUser>> GetRoomList(string Channel)
        {
            string unparsed = await CommonClient.GetAsync(new Uri($"{Url}chat/{CommonClient.Escape(Client)}/{CommonClient.Escape(Channel)}/room_list"), HttpClient);
            IList<ChatUser> roomList = JsonConvert.DeserializeObject<IList<ChatUser>>(unparsed);
            return roomList;
        }
    }
}
