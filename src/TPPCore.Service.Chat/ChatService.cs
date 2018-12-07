using log4net;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.ChatProviders.Providers.Dummy;
using TPPCore.Service.Common;
using TPPCore.ChatProviders.Providers.Irc;
using TPPCore.ChatProviders.Twitch;
using TPPCore.ChatProviders;
using TPPCore.ChatProviders.DataModels;

namespace TPPCore.Service.Chat
{
    public class ChatService : IServiceAsync
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceContext context;
        private ChatFacade chatFacade;
        private WebHandler webHandler;
        private ProviderContext providerContext;

        private List<IProvider> providers;

        public ChatService()
        {
        }

        public void Initialize(ServiceContext context)
        {
            this.context = context;

            chatFacade = new ChatFacade(context);
            webHandler = new WebHandler(chatFacade);

            providerContext = new ProviderContext(context, chatFacade);

            context.RestfulServer.UseRoute((RouteBuilder routeBuilder) =>
            {
                routeBuilder
                    .MapGet("client/{client}/user_id", webHandler.GetUserId)
                    .MapGet("client/{client}/username", webHandler.GetUsername)
                    .MapPost("chat/{client}/{channel}/send", webHandler.PostSendMessage)
                    .MapPost("private_chat/{client}/{user}/send", webHandler.PostSendPrivateMessage)
                    .MapPost("chat/{client}/{channel}/timeout", webHandler.TimeoutUser)
                    .MapPost("chat/{client}/{channel}/ban", webHandler.BanUser)
                    .MapGet("chat/{client}/{channel}/room_list", webHandler.GetRoomList)
                    ;
            });

            providers = new List<IProvider>();
            createProviders();
        }

        public void Run()
        {
            RunAsync().Wait();
        }

        public async Task RunAsync()
        {
            var runningTasks = new List<Task>();

            foreach (var provider in providers)
            {
                if (provider is IProviderThreaded) {
                    var providerThreaded = (IProviderThreaded) provider;
                    var task = Task.Run(() => providerThreaded.Run());
                    runningTasks.Add(task);
                } else {
                    var providerAsync = (IProviderAsync) provider;
                    runningTasks.Add(providerAsync.Run());
                }
            }

            logger.InfoFormat("Running {0} provider(s)", providers.Count);
            await Task.WhenAll(runningTasks);
            logger.Info("Providers have stopped.");
        }

        public void Shutdown()
        {
            foreach (var provider in providers)
            {
                provider.Shutdown();
            }
        }

        private void createProviders()
        {
            var clientNames = context.ConfigReader.GetCheckedValue<List<ChatServiceConfig.ChatConfig.ClientConfig>, ChatServiceConfig>(
                "chat", "clients");

            foreach (var clientName in clientNames)
            {
                var provider = newProvider(clientName.provider);

                logger.InfoFormat("Configuring client {0} with provider {1}",
                    clientName.client, provider.ProviderName);
                provider.Configure(clientName.client, providerContext);
                providers.Add(provider);
                chatFacade.RegisterProvider(provider);
            }
        }

        private IProvider newProvider(string providerName)
        {
            switch (providerName)
            {
                case "dummy":
                    return new DummyProvider();
                case "irc":
                    return new IrcProvider();
                case "twitch":
                    return new TwitchProvider();
                default:
                    throw new System.NotImplementedException(
                        $"IRC provider {providerName} not implemented.");
            }
        }
    }
}
