using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
            string unparsed = await CommonClient.GetAsync(new Uri (Url + "client/" + Client + "/user_id"), HttpClient);
            JObject parsed = JObject.Parse(unparsed);
            return parsed.Value<string>("userId");
        }

        public async Task<string> GetUserName()
        {
            string unparsed = await CommonClient.GetAsync(new Uri (Url + "client/" + Client + "/username"), HttpClient);
            JObject parsed = JObject.Parse(unparsed);
            return parsed.Value<string>("username");
        }

        public async Task SendMessage(string Channel, string Message)
        {
            PostMessage postMessage = new PostMessage { Channel = Channel, ClientName = Client, Message = Message };
            string message = JsonConvert.SerializeObject(postMessage);
            await CommonClient.PostAsync(new Uri(Url + "chat/" + Client + "/" + Channel + "/send"), message, HttpClient);
        }

        public async Task SendPrivateMessage(string User, string Message)
        {
            PostPrivateMessage postPrivateMessage = new PostPrivateMessage { User = User, ClientName = Client, Message = Message };
            string message = JsonConvert.SerializeObject(postPrivateMessage);
            await CommonClient.PostAsync(new Uri(Url + "private_chat/" + Client + "/" + User + "/send"), message, HttpClient);
        }

        public async Task<PostRoomList> GetRoomList(string Channel)
        {
            string unparsed = await CommonClient.GetAsync(new Uri(Url + "chat/" + Client + "/" + Channel + "/room_list"), HttpClient);
            PostRoomList roomList = JsonConvert.DeserializeObject<PostRoomList>(unparsed);
            return roomList;
        }
    }
}
