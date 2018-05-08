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
            var jsonDoc = JsonConvert.SerializeObject(model.RecentMessages);

            await context.RespondStringAsync(jsonDoc);
        }

        public async Task GetCurrent(HttpContext context)
        {
            var jsonDoc = JsonConvert.SerializeObject(model.CurrentMessage);

            await context.RespondStringAsync(jsonDoc);
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
