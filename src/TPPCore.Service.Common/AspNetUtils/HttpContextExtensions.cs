using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace TPPCore.Service.Common.AspNetUtils
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Sets the appropriate HTTP headers and writes the given JSON object.
        /// </summary>
        public async static Task RespondJsonAsync(this HttpContext context, JObject doc)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(doc.ToString());
        }
        /// <summary>
        /// Sets the appropriate HTTP headers and writes the given string object.
        /// </summary>
        public async static Task RespondStringAsync(this HttpContext context, string doc)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(doc);
        }

        /// <summary>
        /// Reads a JSON formatted document body from a POST request.
        /// </summary>
        public async static Task<JObject> ReadJsonAsync(this HttpContext context)
        {
            var reader = new StreamReader(context.Request.Body);
            var text = await reader.ReadToEndAsync();
            var jsonDoc = JObject.Parse(text);
            return jsonDoc;
        }

        /// <summary>
        /// Reads a string body from a POST request.
        /// </summary>
        public async static Task<string> ReadStringAsync(this HttpContext context)
        {
            var reader = new StreamReader(context.Request.Body);
            var text = await reader.ReadToEndAsync();
            return text;
        }
    }
}
