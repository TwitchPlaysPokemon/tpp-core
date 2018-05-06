using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json.Linq;
using TPPCore.ChatProviders.DataModels;

namespace TPPCore.ChatProviders.Twitch
{
    public class TwitchProvider : IProviderAsync
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string roomListUrl = "https://tmi.twitch.tv/group/user/{0}/chatters";

        public string ClientName { get { return twitchIrcProvider.ClientName; } }
        public string ProviderName { get { return "twitch"; } }

        private TwitchIrcProvider twitchIrcProvider;
        private HttpClient httpClient;

        public TwitchProvider()
        {
            twitchIrcProvider = new TwitchIrcProvider();
            httpClient = new HttpClient();
        }

        public void Configure(string clientName, ProviderContext providerContext)
        {
            twitchIrcProvider.Configure(clientName, providerContext);
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

        public async Task<PostRoomList> GetRoomList(string channel)
        {
            var users = (await twitchIrcProvider.GetRoomList(channel)).Viewers;

            // TODO: Customize the IRC client's ChannelTracker to grab the
            // user-id from the tags.
            users.ForEach(item => { item.UserId = null; item.Nickname = null; });

            var url = string.Format(roomListUrl, channel.TrimStart('#'));
            var response = await httpClient.GetAsync(url);
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

            PostRoomList roomList;

            List<ChatUser> Viewers = new List<ChatUser> { };
            List<ChatUser> Moderators = new List<ChatUser> { };
            List<ChatUser> GlobalMods = new List<ChatUser> { };
            List<ChatUser> Admins = new List<ChatUser> { };
            List<ChatUser> Staff = new List<ChatUser> { };

            foreach (string viewer in viewers)
            {
                var user = new ChatUser() { Username = viewer };
                Viewers.Add(user);
                users.Add(user);
            }
            foreach (string moderator in moderators)
            {
                var user = new ChatUser() { Username = moderator };
                Moderators.Add(user);
                users.Add(user);
            }
            foreach (string globalmoderator in global_mods)
            {
                var user = new ChatUser() { Username = globalmoderator };
                GlobalMods.Add(user);
                users.Add(user);
            }
            foreach (string admin in admins)
            {
                var user = new ChatUser() { Username = admin };
                Admins.Add(user);
                users.Add(user);
            }
            foreach (string staffmember in staff)
            {
                var user = new ChatUser() { Username = staffmember };
                Staff.Add(user);
                users.Add(user);
            }

            var processedUsers = users.Distinct(new ChatUserEqualityComparer()).ToList();

            roomList = new PostRoomList()
            {
                NumUsers = processedUsers.Count,
                Viewers = Viewers,
                Moderators = Moderators,
                GlobalMods = GlobalMods,
                Admins = Admins,
                Staff = Staff,
                ClientName = ClientName,
            };
            return roomList;
        }
    }
}
