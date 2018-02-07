using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json.Linq;
using TPPCore.Service.Chat.DataModels;

namespace TPPCore.Service.Chat.Twitch
{
    public class TwitchProvider : IProviderAsync
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string roomListUrl = "https://tmi.twitch.tv/group/user/twitchplayspokemon/chatters";

        public string Name { get { return "twitch"; } }

        private TwitchIrcProvider twitchIrcProvider;
        private HttpClient httpClient;

        public TwitchProvider()
        {
            twitchIrcProvider = new TwitchIrcProvider();
            httpClient = new HttpClient();
        }

        public void Configure(ProviderContext providerContext)
        {
            twitchIrcProvider.Configure(providerContext);
        }

        public async Task Run()
        {
            await twitchIrcProvider.Run();
        }

        public void Shutdown()
        {
            twitchIrcProvider.Shutdown();
        }

        public string GetUserId()
        {
            throw new System.NotImplementedException();
        }

        public string GetUsername()
        {
            return twitchIrcProvider.GetUsername();
        }

        public async Task SendMessage(string channel, string message)
        {
            await twitchIrcProvider.SendMessage(channel, message);
        }

        public async Task SendPrivateMessage(string user, string message)
        {
            await SendMessage("#jtv", $".w {user} message");
        }

        public async Task<IList<ChatUser>> GetRoomList(string channel)
        {
            var users = (await twitchIrcProvider.GetRoomList(channel)).ToList();

            var response = await httpClient.GetAsync(roomListUrl);
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JObject.Parse(jsonString);

            var moderators = jsonDoc.SelectToken("chatters.moderators")
                .Select(item => (string) item);
            var staff = jsonDoc.SelectToken("chatters.staff")
                .Select(item => (string) item);
            var admins = jsonDoc.SelectToken("chatters.admins")
                .Select(item => (string) item);
            var global_mods = jsonDoc.SelectToken("chatters.global_mods")
                .Select(item => (string) item);
            var viewers = jsonDoc.SelectToken("chatters.viewers")
                .Select(item => (string) item);

            foreach (var usernames in new[] {moderators, staff, admins, global_mods, viewers})
            {
                foreach (var username in usernames)
                {
                    var user = new ChatUser() { Username = username };
                    users.Add(user);
                }
            }

            return users.Distinct(new ChatUserEqualityComparer()).ToList();
        }
    }
}
