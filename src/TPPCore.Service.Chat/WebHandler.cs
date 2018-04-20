using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using TPPCore.Service.Common.AspNetUtils;
using TPPCore.ChatProviders;

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
            var userId = chatFacade.GetUserId(client);
            var jsonDoc = JObject.FromObject(new { userId = userId });

            await context.RespondJsonAsync(jsonDoc);
        }

        public async Task GetUsername(HttpContext context)
        {
            var client = (string) context.GetRouteValue("client");
            var username = chatFacade.GetUserId(client);
            var jsonDoc = JObject.FromObject(new { username = username });

            await context.RespondJsonAsync(jsonDoc);
        }

        public async Task PostSendMessage(HttpContext context)
        {
            var client = (string) context.GetRouteValue("client");
            var channel = (string) context.GetRouteValue("channel");
            var inputDoc = await context.ReadJsonAsync();
            var message = inputDoc.Value<string>("message");

            await chatFacade.SendMessage(client, channel, message);
            await context.RespondJsonAsync(inputDoc);
        }

        public async Task PostSendPrivateMessage(HttpContext context)
        {
            var client = (string) context.GetRouteValue("client");
            var user = (string) context.GetRouteValue("user");
            var inputDoc = await context.ReadJsonAsync();
            var message = inputDoc.Value<string>("message");

            await chatFacade.SendPrivateMessage(client, user, message);
            await context.RespondJsonAsync(inputDoc);
        }

        public async Task GetRoomList(HttpContext context)
        {
            var client = (string) context.GetRouteValue("client");
            var channel = (string) context.GetRouteValue("channel");

            var users = await chatFacade.GetRoomList(client, channel);

            var jsonDoc = JObject.FromObject(new
            {
                users = users.Select(user => user.ToJObject())
            });

            await context.RespondJsonAsync(jsonDoc);
        }
    }
}
