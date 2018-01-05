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

            var httpClient = new HttpClient();

            var uri = new Uri(runner.Runner.Context.RestfulServer.Context.GetUri(),
                "/message/current");
            var response = await httpClient.GetAsync(uri);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jsonDoc = JObject.Parse(body);

            Assert.Equal("hello world!", jsonDoc.GetValue("message").ToString());

            await runner.TearDownAsync();
        }

        [Fact]
        public async Task TestNewMessage()
        {
            var runner = newServiceRunner();
            await runner.SetUpAsync();

            var httpClient = new HttpClient();

            var jsonDoc = new JObject();
            jsonDoc.Add("message", "wow");

            var uri = new Uri(runner.Runner.Context.RestfulServer.Context.GetUri(),
                "/message/new");

            var content = new StringContent(jsonDoc.ToString());
            var response = await httpClient.PostAsync(uri, content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await runner.TearDownAsync();
        }
    }
}
