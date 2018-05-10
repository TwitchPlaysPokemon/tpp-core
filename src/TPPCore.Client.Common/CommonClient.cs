using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TPPCore.Client.Common
{
    public class CommonClient
    {
        public static async Task<string> GetAsync(Uri requestUri, RestfulClient HttpClient)
        {
            Task<string> task = HttpClient.GetStringAsync(requestUri);
            string msg = await task;
            return msg;
        }

        public static async Task PostAsync(Uri requestUri, string message, RestfulClient HttpClient)
        {
            HttpResponseMessage msg = await HttpClient.PostAsync(requestUri, new StringContent(message));
            if (!msg.IsSuccessStatusCode)
                throw new HttpRequestException();
        }

        public static string Escape(string toEscape)
        {
            return Uri.EscapeDataString(toEscape);
        }
    }
}
