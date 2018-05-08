using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TPPCore.Client.Common
{
    public class RestfulClientResult
    {
        public HttpResponseMessage Response;
        public string Serialized;
    }

    public class RestfulClient : HttpClient
    {
        public RestfulClient()
        {
        }

        public RestfulClient(HttpMessageHandler handler) : base(handler)
        {
        }

        public RestfulClient(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
        {
        }

        public async Task<RestfulClientResult> GetJsonAsync(Uri uri)
        {
            var response = await GetAsync(uri);
            var body = await response.Content.ReadAsStringAsync();

            return new RestfulClientResult()
            {
                Response = response,
                Serialized = body
            };
        }

        public async Task<RestfulClientResult> PostJsonAsync(
            Uri uri, string Seralized)
        {
            var content = new StringContent(Seralized);
            var response = await PostAsync(uri, content);
            var body = await response.Content.ReadAsStringAsync();

            return new RestfulClientResult()
            {
                Response = response,
                Serialized = body
            };
        }
    }
}
