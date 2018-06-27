using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TPPCore.Service.Common;
using TPPCore.Service.Common.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace TPPCore.Service.Emotes.Test
{
    public class RestfulTest
    {
        private readonly ITestOutputHelper output;

        public RestfulTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        private ServiceRunnerOptions GetOptions()
        {
            var options = ServiceTestRunner.GetDefaultOptions();
            var assembly = typeof(RestfulTest).Assembly;

            output.WriteLine(assembly.GetManifestResourceNames().Length.ToString());
            output.WriteLine(string.Join(", ", assembly.GetManifestResourceNames()));

            options.ConfigStream = assembly.GetManifestResourceStream(
                "TPPCore.Service.Emotes.Tests.test_files.emote_config.yaml");
            Assert.NotNull(options.ConfigStream);

            return options;
        }

        private ServiceTestRunner NewServiceRunner()
        {
            return new ServiceTestRunner(new EmoteService());
        }

        [Fact]
        public async Task EmoteFromIdTest()
        {
            if (!Directory.Exists("cache/"))
                Directory.CreateDirectory("cache/");

            if (!File.Exists("cache/emotes.json"))
            {
                string json = File.ReadAllText("../../../test_files/cache.json");
                File.WriteAllText("cache/emotes.json", json);
            }

            var runner = NewServiceRunner();
            await runner.SetUpAsync(GetOptions());

            await Task.Delay(10000);

            var httpClient = runner.Runner.Context.RestfulClient;
            var result = await httpClient.GetAsync(runner.Runner.Context.RestfulServer.Context.GetUri().ToString() + "emote/fromid/25");
            if (!result.IsSuccessStatusCode)
                throw new HttpRequestException("Error " + result.StatusCode.ToString());
            string jsonstring = await result.Content.ReadAsStringAsync();

            TwitchEmote info = JsonConvert.DeserializeObject<TwitchEmote>(jsonstring);

            Assert.Equal("kappa", info.Code.ToLower());
            await runner.TearDownAsync();
        }

        [Fact]
        public async Task EmoteFromCodeTest()
        {
            if (!Directory.Exists("cache/"))
                Directory.CreateDirectory("cache/");

            if (!File.Exists("cache/emotes.json"))
            {
                string json = File.ReadAllText("../../../test_files/cache.json");
                File.WriteAllText("cache/emotes.json", json);
            }

            var runner = NewServiceRunner();
            await runner.SetUpAsync(GetOptions());

            await Task.Delay(10000);

            var httpClient = runner.Runner.Context.RestfulClient;

            var result = await httpClient.GetAsync(runner.Runner.Context.RestfulServer.Context.GetUri().ToString() + "emote/fromcode/Kappa");
            string jsonstring = await result.Content.ReadAsStringAsync();

            TwitchEmote info = JsonConvert.DeserializeObject<TwitchEmote>(jsonstring);

            Assert.Equal(25, info.Id);
            await runner.TearDownAsync();
        }

        [Fact]
        public async Task FindEmoteTest()
        {
            if (!Directory.Exists("cache/"))
                Directory.CreateDirectory("cache/");

            if (!File.Exists("cache/emotes.json"))
            {
                string json = File.ReadAllText("../../../test_files/cache.json");
                File.WriteAllText("cache/emotes.json", json);
            }


            var runner = NewServiceRunner();
            await runner.SetUpAsync(GetOptions());

            await Task.Delay(10000);

            var httpClient = runner.Runner.Context.RestfulClient;

            var result = await httpClient.GetAsync(runner.Runner.Context.RestfulServer.Context.GetUri().ToString() + "emote/findin/Kappa");

            string jsonstring = await result.Content.ReadAsStringAsync();

            List<TwitchEmote> emoteinfo = JsonConvert.DeserializeObject<List<TwitchEmote>>(jsonstring);

            Assert.Equal("https://static-cdn.jtvnw.net/emoticons/v1/25/1.0", emoteinfo[0].ImageUrls[0]);
            Assert.Equal("https://static-cdn.jtvnw.net/emoticons/v1/25/2.0", emoteinfo[0].ImageUrls[1]);
            Assert.Equal("https://static-cdn.jtvnw.net/emoticons/v1/25/3.0", emoteinfo[0].ImageUrls[2]);

            result = await httpClient.GetAsync(runner.Runner.Context.RestfulServer.Context.GetUri().ToString() + "emote/findin/kappa");

            jsonstring = await result.Content.ReadAsStringAsync();

            emoteinfo = JsonConvert.DeserializeObject<List<TwitchEmote>>(jsonstring);

            Assert.True(emoteinfo.Where(x => x.Code == "kappa" || x.Code == "Kappa").Count() == 0);

            await runner.TearDownAsync();
        }
    }
}
