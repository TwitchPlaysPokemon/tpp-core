using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Example.Parrot
{
    class ParrotWebHandler
    {
        private readonly Model model;

        public ParrotWebHandler(Model model)
        {
            this.model = model;
        }

        public async Task GetRecent(HttpContext context)
        {
            var jsonDoc = new JObject();
            jsonDoc.Add("messages", new JArray(model.RecentMessages));

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(jsonDoc.ToString());
        }

        public async Task GetCurrent(HttpContext context)
        {
            var jsonDoc = new JObject();
            jsonDoc.Add("message", model.CurrentMessage);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(jsonDoc.ToString());
        }

        public async Task PostMessage(HttpContext context)
        {
            var reader = new StreamReader(context.Request.Body);
            var text = await reader.ReadToEndAsync();
            var jsonDoc = JObject.Parse(text);

            var message = jsonDoc.GetValue("message").ToString();

            model.RepeatNewMessage(message);
        }
    }
}
