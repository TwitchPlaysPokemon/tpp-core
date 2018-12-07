using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TPPCore.Service.Common;
using TPPCore.Service.Common.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace TPPCore.Service.Emotes.Test
{
    public class RestfulTest : IClassFixture<ServiceFixture>
    {
        private readonly ITestOutputHelper output;
        private readonly ServiceFixture serviceFixture;

        #region Shortcuts
        private ServiceTestRunner Runner => serviceFixture.Runner;
        private Client.Common.RestfulClient Client => Runner.Runner.Context.RestfulClient;
        private string ClientUrl => Runner.Runner.Context.RestfulServer.Context.GetUri().ToString();
        #endregion

        #region Endpoints
        private const string FromId = "emote/fromid/";
        private const string FromCode = "emote/fromcode/";
        private const string FindIn = "emote/findin/";
        #endregion

        public RestfulTest(ITestOutputHelper output, ServiceFixture fixture)
        {
            this.output = output;
            this.serviceFixture = fixture;
        }

        #region API Wrappers
        private async Task<T> Get<T>(string url, string data)
        {
            var response = await Client.GetAsync(ClientUrl + url + Uri.EscapeDataString(data));
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("Error " + response.StatusCode.ToString());
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        private async Task<T> Post<T>(string url, string data)
        {
            var response = await Client.PostAsync(ClientUrl + url, new StringContent(data));
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("Error " + response.StatusCode.ToString());
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }
        #endregion

        [Fact]
        public async Task FromIdTest()
        {
            var info = await Get<EmoteInfo>(FromId, "25");
            Assert.Equal("Kappa", info.Code);
        }

        [Fact]
        public async Task FromCodeTest()
        {
            var info = await Get<EmoteInfo>(FromCode, "Kappa");
            Assert.Equal(25, info.Id);
        }

        [Fact]
        public async Task CaseSensitiveTest()
        {
            //This test will break if there is ever a lowercase Kappa
            var info = await Get<EmoteInfo>(FromCode, "kappa");
            Assert.Null(info);
        }

        [Fact]
        public async Task TwitchImagesTest()
        {
            var info = await Get<EmoteInfo>(FromCode, "Kappa");
            Assert.Equal("https://static-cdn.jtvnw.net/emoticons/v1/25/1.0", info.ImageUrls[0]);
            Assert.Equal("https://static-cdn.jtvnw.net/emoticons/v1/25/2.0", info.ImageUrls[1]);
            Assert.Equal("https://static-cdn.jtvnw.net/emoticons/v1/25/3.0", info.ImageUrls[2]);
        }

        [Fact]
        public async Task FindInTest()
        {
            // Test will fail if Hello or there become emotes
            var emoteInfo = await Post<List<EmoteInfo>>(FindIn, "Hello there Kappa");
            Assert.Single(emoteInfo);
            Assert.Equal("Kappa", emoteInfo[0].Code);
        }

        [Fact]
        public async Task FindInCaseSensitiveTest()
        {
            //This test will break if there is ever a lowercase Kappa
            var emoteInfo = await Post<List<EmoteInfo>>(FindIn, "kappa");
            Assert.Empty(emoteInfo);
        }

        [Fact]
        public async Task TroublesomeEmotesTest()
        {
            var emoteInfo = await Post<List<EmoteInfo>>(FindIn, "<3 :) :\\");
            Assert.Equal("<3", emoteInfo[0].Code);
            Assert.Equal(":)", emoteInfo[1].Code);
            Assert.Equal(":\\", emoteInfo[2].Code);
        }

        [Fact]
        public async Task FindInGetTest()
        {
            var emoteInfo = await Get<List<EmoteInfo>>(FindIn, "test :)");
            Assert.NotEmpty(emoteInfo);
            Assert.Equal(1, emoteInfo[0].Id);
        }

    }

    public class TestEmoteService : EmoteService
    {
        public void WaitForEmotes()
        {
            emoteHandler.GetEmotes(new System.Threading.CancellationToken()).Wait();
        }
    }

    public class ServiceFixture : IDisposable
    {
        public ServiceTestRunner Runner;

        public ServiceFixture()
        {
            if (!Directory.Exists("cache/"))
                Directory.CreateDirectory("cache/");

            if (!File.Exists("cache/emotes.json"))
            {
                string json = File.ReadAllText("../../../test_files/cache.json");
                File.WriteAllText("cache/emotes.json", json);
            }
            Runner = new ServiceTestRunner(new TestEmoteService());
            Runner.Service.Shutdown(); //don't bother running, we'll load emotes manually
            Runner.SetUp(GetOptions());
            (Runner.Service as TestEmoteService).WaitForEmotes();
        }
        public void Dispose()
        {
            Runner.TearDown();
        }
        private ServiceRunnerOptions GetOptions()
        {
            var options = ServiceTestRunner.GetDefaultOptions();
            var assembly = typeof(RestfulTest).Assembly;

            options.ConfigStream = assembly.GetManifestResourceStream(
                "TPPCore.Service.Emotes.Tests.test_files.emote_config.json");
            Assert.NotNull(options.ConfigStream);

            return options;
        }
    }

    public class EmoteInfo
    {
        public string Code;
        public int Id;
        public string[] ImageUrls;
    }

}
