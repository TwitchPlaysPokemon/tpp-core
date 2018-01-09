using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using TPPCore.Service.Common;
using TPPCore.Service.Common.AspNetUtils;

namespace TPPCore.Service.Example.Parrot
{
    /// <summary>
    /// RESTful endpoint handlers
    /// </summary>
    class ParrotWebHandler
    {
        private readonly Model model;

        public ParrotWebHandler(Model model)
        {
            this.model = model;
        }

        public async Task GetRecent(HttpContext context)
        {
            var jsonDoc = JObject.FromObject(new { messages = model.RecentMessages });

            await context.RespondJsonAsync(jsonDoc);
        }

        public async Task GetCurrent(HttpContext context)
        {
            var jsonDoc = JObject.FromObject(new { message = model.CurrentMessage });

            await context.RespondJsonAsync(jsonDoc);
        }

        public async Task PostMessage(HttpContext context)
        {
            var jsonDoc = await context.ReadJsonAsync();

            var message = jsonDoc.GetValue("message").ToString();

            model.RepeatNewMessage(message);
        }
    }
}
