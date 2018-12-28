using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using TPPCore.ChatProviders.DataModels;

namespace TPPCore.ChatProviders.Twitch
{
    public class TwitchProvider : IProviderAsync
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string roomListUrl = "https://tmi.twitch.tv/group/user/{0}/chatters";
        private const string NewApiUrl = "https://api.twitch.tv/helix/";

        public string ClientName { get { return twitchIrcProvider.ClientName; } }
        public string ProviderName { get { return "twitch"; } }

        private TwitchIrcProvider twitchIrcProvider;
        private HttpClient httpClient;
        private ProviderContext context;

        public TwitchProvider()
        {
            twitchIrcProvider = new TwitchIrcProvider();
            httpClient = new HttpClient();
        }

        public void Configure(string clientName, ProviderContext providerContext)
        {
            context = providerContext;
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

        public async Task<string> GetUserId()
        {
            ChatServiceConfig.ChatConfig.ClientConfig provider = context.Service.ConfigReader
                .GetCheckedValue<List<ChatServiceConfig.ChatConfig.ClientConfig>, ChatServiceConfig>()
                .First(x => x.provider == ClientName);
            string clientID = provider.client_id;
            string url = NewApiUrl + "/users?login=" + GetUsername();
            httpClient.DefaultRequestHeaders.Add("client-id", clientID);

            HttpResponseMessage response = await httpClient.GetAsync(url);
            string JsonString = await response.Content.ReadAsStringAsync();
            UserApiResponse userResponse = JsonConvert.DeserializeObject<UserApiResponse>(JsonString);

            string UserID = userResponse.data[0].id;

            httpClient.DefaultRequestHeaders.Remove("client-id");

            return UserID;
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
            await SendMessage("#jtv", $".w {user} {message}");
        }

        public async Task TimeoutUser(ChatUser user, string reason, int duration, string channel)
        {
            await SendMessage(channel, $".timeout {user.Username} {duration} {reason}");
        }

        public async Task BanUser(ChatUser user, string reason, string channel)
        {
            await SendMessage(channel, $".ban {user.Username} {reason}");
        }

        public async Task<IList<ChatUser>> GetRoomList(string channel)
        {
            var users = (await twitchIrcProvider.GetRoomList(channel)).ToList();

            // TODO: Customize the IRC client's ChannelTracker to grab the
            // user-id from the tags.
            users.ForEach(item => { item.UserId = null; item.Nickname = null; });

            var url = string.Format(roomListUrl, channel.TrimStart('#'));
            var response = await httpClient.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();
            ChatList chatList = JsonConvert.DeserializeObject<ChatList>(jsonString);

            var moderators = chatList.chatters.moderators;
            var staff = chatList.chatters.staff;
            var admins = chatList.chatters.admins;
            var global_mods = chatList.chatters.global_mods;
            var viewers = chatList.chatters.viewers;

            foreach (var username in viewers)
            {
                var user = new ChatUser() { Username = username, AccessLevel = AccessLevel.Viewer };
                users.Add(user);
            }

            foreach (var username in moderators)
            {
                var user = new ChatUser() { Username = username, AccessLevel = AccessLevel.Moderator };
                users.Add(user);
            }

            foreach (var usernames in new[] { global_mods, admins, staff })
            {
                foreach (var username in usernames)
                {
                    var user = new ChatUser() { Username = username, AccessLevel = AccessLevel.Staff };
                    users.Add(user);
                }
            }

            return users.Distinct(new ChatUserEqualityComparer()).ToList();
        }
    }
}
