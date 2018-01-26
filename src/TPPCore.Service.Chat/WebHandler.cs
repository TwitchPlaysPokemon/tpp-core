using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using TPPCore.Service.Common.AspNetUtils;

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
            var provider = (string) context.GetRouteValue("provider");
            var userId = chatFacade.GetUserId(provider);
            var jsonDoc = JObject.FromObject(new { userId = userId });

            await context.RespondJsonAsync(jsonDoc);
        }

        public async Task GetUsername(HttpContext context)
        {
            var provider = (string) context.GetRouteValue("provider");
            var username = chatFacade.GetUserId(provider);
            var jsonDoc = JObject.FromObject(new { username = username });

            await context.RespondJsonAsync(jsonDoc);
        }

        public async Task PostSendMessage(HttpContext context)
        {
            var provider = (string) context.GetRouteValue("provider");
            var channel = (string) context.GetRouteValue("channel");
            var inputDoc = await context.ReadJsonAsync();
            var message = inputDoc.Value<string>("message");

            await chatFacade.SendMessage(provider, channel, message);
            await context.RespondJsonAsync(inputDoc);
        }

        public async Task PostSendPrivateMessage(HttpContext context)
        {
            var provider = (string) context.GetRouteValue("provider");
            var user = (string) context.GetRouteValue("user");
            var inputDoc = await context.ReadJsonAsync();
            var message = inputDoc.Value<string>("message");

            await chatFacade.SendPrivateMessage(provider, user, message);
            await context.RespondJsonAsync(inputDoc);
        }

        public async Task GetRoomList(HttpContext context)
        {
            var provider = (string) context.GetRouteValue("provider");
            var channel = (string) context.GetRouteValue("channel");

            var users = await chatFacade.GetRoomList(provider, channel);

            var jsonDoc = JObject.FromObject(new
            {
                users = users.Select(user => user.ToJObject())
            });

            await context.RespondJsonAsync(jsonDoc);
        }
    }
}
