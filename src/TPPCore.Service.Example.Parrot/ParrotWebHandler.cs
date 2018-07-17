using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System.Threading.Tasks;
using TPPCore.Service.Common.AspNetUtils;

namespace TPPCore.Service.Example.Parrot
{
    /// <summary>
    /// RESTful endpoint handlers
    /// </summary>
    class ParrotWebHandler
    {
        private readonly Model model;
        private readonly DatabaseHandler handler;

        public ParrotWebHandler(Model model, DatabaseHandler handler)
        {
            this.model = model;
            this.handler = handler;
        }

        public async Task GetRecent(HttpContext context)
        {
            var jsonDoc = JsonConvert.SerializeObject(model.RecentMessages);

            await context.RespondStringAsync(jsonDoc);
        }

        public async Task SaveToDatabase(string serialized)
        {
            string unserialized = JsonConvert.DeserializeObject<string>(serialized);
            await handler.SaveToDatabase(unserialized);
        }

        public async Task GetCurrent(HttpContext context)
        {
            var jsonDoc = JsonConvert.SerializeObject(model.CurrentMessage);

            await context.RespondStringAsync(jsonDoc);
        }

        public async Task GetMaxId(HttpContext httpContext)
        {
            string jsondoc = JsonConvert.SerializeObject(await handler.GetMaxId());

            await httpContext.RespondStringAsync(jsondoc);
        }

        public async Task GetContents(HttpContext httpContext)
        {
            int.TryParse((string)httpContext.GetRouteValue("id"), out int result);
            string contents = await handler.GetContents(result);
            string jsondoc = JsonConvert.SerializeObject(contents);

            await httpContext.RespondStringAsync(jsondoc);
        }

        public async Task PostMessage(HttpContext context)
        {
            var jsonDoc = await context.ReadStringAsync();

            var message = JsonConvert.DeserializeObject<string>(jsonDoc);

            model.RepeatNewMessage(message);

            await context.RespondStringAsync(string.Empty);
        }
    }
}
