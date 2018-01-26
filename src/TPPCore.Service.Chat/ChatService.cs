using log4net;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Threading.Tasks;
using TPPCore.Service.Chat.Providers.Dummy;
using TPPCore.Service.Common;
using TPPCore.Service.Chat.Providers.Irc;

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
                    .MapGet("provider/{provider}/user_id", webHandler.GetUserId)
                    .MapGet("provider/{provider}/username", webHandler.GetUsername)
                    .MapPost("chat/{provider}/{channel}/send", webHandler.PostSendMessage)
                    .MapPost("private_chat/{provider}/{user}/send", webHandler.PostSendPrivateMessage)
                    .MapGet("chat/{provider}/{channel}/room_list", webHandler.GetRoomList)
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
            var providerNames = context.ConfigReader.GetCheckedValue<string[]>(
                new[] {"chat", "providers"});

            foreach (var providerName in providerNames)
            {
                var provider = newProvider(providerName);

                logger.InfoFormat("Configuring provider {0}", provider.Name);
                provider.Configure(providerContext);
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
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}
