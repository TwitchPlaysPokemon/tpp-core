using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TPPCore.Service.Common;
using TPPCore.Service.Example.Parrot;
using Xunit;

namespace TPPCore.Service.Example.Parrot.Tests
{
    public class RestfulTest
    {
        ParrotService service;
        ServiceRunner runner;

        public RestfulTest() {
            service = new ParrotService();
            runner = new ServiceRunner(service);

            runner.Configure();
        }

        [Fact]
        public async Task TestCurrent()
        {
            await runner.StartRestfulServerAsync();
            var runTask = runner.RunAsync();
            var httpClient = new HttpClient();

            var uri = new Uri(runner.Context.RestfulServer.Context.GetUri(),
                "/message/current");
            var response = await httpClient.GetAsync(uri);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jsonDoc = JObject.Parse(body);

            Assert.Equal("hello world!", jsonDoc.GetValue("message").ToString());

            runner.Stop();
            await runTask;
            await runner.StopRestfulServerAsync();
        }

        [Fact]
        public async Task TestNewMessage()
        {
            await runner.StartRestfulServerAsync();
            var runTask = runner.RunAsync();
            var httpClient = new HttpClient();

            var jsonDoc = new JObject();
            jsonDoc.Add("message", "wow");

            var uri = new Uri(runner.Context.RestfulServer.Context.GetUri(),
                "/message/new");

            var content = new StringContent(jsonDoc.ToString());
            var response = await httpClient.PostAsync(uri, content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            runner.Stop();
            await runTask;
            await runner.StopRestfulServerAsync();
        }
    }
}
