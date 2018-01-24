using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TPPCore.Service.Common;
using TPPCore.Service.Common.TestUtils;
using TPPCore.Service.Example.Parrot;
using Xunit;

namespace TPPCore.Service.Example.Parrot.Tests
{
    public class RestfulTest
    {
        public RestfulTest() {
        }

        private ServiceTestRunner newServiceRunner()
        {
            return new ServiceTestRunner(new ParrotService());
        }

        [Fact]
        public async Task TestCurrent()
        {
            var runner = newServiceRunner();
            await runner.SetUpAsync();

            var httpClient = runner.Runner.Context.RestfulClient;
            var uri = new Uri(runner.Runner.Context.RestfulServer.Context.GetUri(),
                "/message/current");
            var result = await httpClient.GetJsonAsync(uri);

            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
            Assert.Equal("hello world!", result.JsonDoc.Value<string>("message"));

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestNewMessage()
        {
            var runner = newServiceRunner();
            await runner.SetUpAsync();

            var httpClient = runner.Runner.Context.RestfulClient;
            var jsonDoc = new JObject();
            jsonDoc.Add("message", "wow");

            var uri = new Uri(runner.Runner.Context.RestfulServer.Context.GetUri(),
                "/message/new");

            var result = await httpClient.PostJsonAsync(uri, jsonDoc);

            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            await runner.TearDownAsync();
        }
    }
}
