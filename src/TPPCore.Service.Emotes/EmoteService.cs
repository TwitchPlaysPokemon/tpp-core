using System;
using Microsoft.AspNetCore.Routing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TPPCore.Service.Common;

namespace TPPCore.Service.Emotes
{
    public class EmoteService : IServiceAsync
    {
        protected EmoteHandler emoteHandler;
        private CancellationTokenSource token = new CancellationTokenSource();
        string fileLocation;
        private string bttvLocation;

        public void Initialize(ServiceContext context)
        {
            string Cachelocation = context.ConfigReader.GetCheckedValue<string, EmotesConfig>("emote", "cache_location");
            if (!Directory.Exists(Cachelocation))
                Directory.CreateDirectory(Cachelocation);
            fileLocation = Cachelocation + "emotes.json";
            bttvLocation = Cachelocation + "bttv.json";
            if (!File.Exists(fileLocation))
                File.Create(fileLocation);
            emoteHandler = new EmoteHandler(context, fileLocation, bttvLocation);

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
                    GC.Collect(); //Yes I know you're not supposed to use GC.Collect(), but it cuts it down from 1.2GB to 600MB of RAM
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(1800000, token.Token);
                    await emoteHandler.GetEmotes(token.Token, false);
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
