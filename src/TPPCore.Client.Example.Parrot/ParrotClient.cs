using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TPPCore.Client.Common;

namespace TPPCore.Client.Example.Parrot
{
    public class ParrotClient
    {
        private string Url;
        private RestfulClient HttpClient;
        public ParrotClient(string Url, RestfulClient HttpClient)
        {
            this.Url = Url;
            this.HttpClient = HttpClient;
        }
        public async Task<ReadOnlyCollection<string>> GetRecent()
        {
            string unparsed = await CommonClient.GetAsync(new Uri(Url + "message/recent"), HttpClient);
            JObject parsed = JObject.Parse(unparsed);
            return parsed.Value<ReadOnlyCollection<string>>("messages");
        }
        public async Task<string> GetCurrent()
        {
            string unparsed = await CommonClient.GetAsync(new Uri(Url + "message/current"), HttpClient);
            JObject parsed = JObject.Parse(unparsed);
            return parsed.Value<string>("message");
        }
        public async Task PostMessage(string message)
        {
            JObject jObject = new JObject
            {
                { "message", message }
            };
            await CommonClient.PostAsync(new Uri(Url + "message/new"), jObject, HttpClient);
        }
    }
}
