using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TPPCore.Service.Common.AspNetUtils;
using TPPCore.ChatProviders;
using TPPCore.ChatProviders.DataModels;
using Newtonsoft.Json;

namespace TPPCore.Service.Chat
{
    public class WebHandler
    {
        private readonly ChatFacade chatFacade;

        public WebHandler(ChatFacade chatFacade)
        {
            this.chatFacade = chatFacade;
        }

        public async Task GetUserId(HttpContext context)
        {
            var client = (string) context.GetRouteValue("client");
            var userId = await chatFacade.GetUserId(client);
            var jsonDoc = JsonConvert.SerializeObject(userId);

            await context.RespondStringAsync(jsonDoc);
        }

        public async Task GetUsername(HttpContext context)
        {
            var client = (string) context.GetRouteValue("client");
            var username = chatFacade.GetUsername(client);
            var jsonDoc = JsonConvert.SerializeObject(username);

            await context.RespondStringAsync(jsonDoc);
        }

        public async Task PostSendMessage(HttpContext context)
        {
            var inputDoc = await context.ReadStringAsync();
            PostMessage postMessage = JsonConvert.DeserializeObject<PostMessage>(inputDoc);
            var client = postMessage.ClientName;
            var channel = postMessage.Channel;
            var message = postMessage.Message;

            await chatFacade.SendMessage(client, channel, message);
            await context.RespondStringAsync(inputDoc);
        }

        public async Task PostSendPrivateMessage(HttpContext context)
        {
            var inputDoc = await context.ReadStringAsync();
            PostPrivateMessage postPrivateMessage = JsonConvert.DeserializeObject<PostPrivateMessage>(inputDoc);
            var client = postPrivateMessage.ClientName;
            var user = postPrivateMessage.User;
            var message = postPrivateMessage.Message;

            await chatFacade.SendPrivateMessage(client, user, message);
            await context.RespondStringAsync(inputDoc);
        }

        public async Task TimeoutUser(HttpContext context)
        {
            var inputDoc = await context.ReadStringAsync();
            PostTimeout postTimeout = JsonConvert.DeserializeObject<PostTimeout>(inputDoc);
            var client = postTimeout.ClientName;
            var user = postTimeout.User;
            var reason = postTimeout.Reason;
            var duration = postTimeout.Duration;
            var channel = postTimeout.Channel;

            await chatFacade.TimeoutUser(client, user, reason, duration, channel);
            await context.RespondStringAsync(inputDoc);
        }

        public async Task BanUser(HttpContext context)
        {
            var inputDoc = await context.ReadStringAsync();
            PostBan postBan = JsonConvert.DeserializeObject<PostBan>(inputDoc);
            var client = postBan.ClientName;
            var user = postBan.User;
            var reason = postBan.Reason;
            var channel = postBan.Channel;

            await chatFacade.BanUser(client, user, reason, channel);
            await context.RespondStringAsync(inputDoc);
        }

        public async Task GetRoomList(HttpContext context)
        {
            var client = (string) context.GetRouteValue("client");
            var channel = (string) context.GetRouteValue("channel");

            var RoomList = await chatFacade.GetRoomList(client, channel);

            string serialized = JsonConvert.SerializeObject(RoomList);
            await context.RespondStringAsync(serialized);
        }
    }
}
