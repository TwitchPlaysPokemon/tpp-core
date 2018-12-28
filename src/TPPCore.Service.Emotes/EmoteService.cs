using Microsoft.AspNetCore.Routing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Emotes
{
    public class EmoteService : IServiceAsync
    {
        private ServiceContext context;
        protected EmoteHandler emoteHandler;
        private CancellationTokenSource token = new CancellationTokenSource();
        string fileLocation;

        public void Initialize(ServiceContext context)
        {
            this.context = context;
            string Cachelocation = context.ConfigReader.GetCheckedValue<string, EmotesConfig>("emote", "cache_location");
            if (!Directory.Exists(Cachelocation))
                Directory.CreateDirectory(Cachelocation);
            fileLocation = Cachelocation + "emotes.json";
            if (!File.Exists(fileLocation))
                File.Create(fileLocation);
            emoteHandler = new EmoteHandler(context, fileLocation);

            context.RestfulServer.UseRoute((RouteBuilder routeBuilder) =>
            {
                routeBuilder
                    .MapGet("emote/fromid/{id}", emoteHandler.GetEmoteFromId)
                    .MapGet("emote/fromcode/{code}", emoteHandler.GetEmoteFromCode)
                    .MapPost("emote/findin", emoteHandler.FindEmotesPost)
                    .MapGet("emote/findin/{text}", emoteHandler.FindEmotesGet)
                    ;
            });
        }

        public void Run()
        {
            RunAsync().Wait();
        }

        public async Task RunAsync()
        {
            await emoteHandler.GetEmotes(token.Token, true);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await emoteHandler.GetEmotes(token.Token, false);
                    await Task.Delay(1800000, token.Token);
                } catch
                {
                    break;
                }
            }
        }

        public void Shutdown()
        {
            token.Cancel();
        }
    }
}
