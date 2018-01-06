using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Common
{
    public static class HttpContextExtensions
    {
        public async static Task RespondJsonAsync(this HttpContext context, JObject doc)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(doc.ToString());
        }

        public async static Task<JObject> ReadJsonAsync(this HttpContext context)
        {
            var reader = new StreamReader(context.Request.Body);
            var text = await reader.ReadToEndAsync();
            var jsonDoc = JObject.Parse(text);
            return jsonDoc;
        }
    }
}
