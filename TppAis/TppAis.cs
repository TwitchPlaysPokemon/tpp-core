using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Configuration;
using Inputting;
using JsonNet.ContractResolvers;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Persistence.Repos;

namespace TppAis
{
    internal class TppAis
    {
        private static void Main(string[] args)
        {
            SetUpLoggingConfig();
            var tpp = new TppAis();
            tpp.Run().Wait();
        }

        private static readonly ILog Log = LogManager.GetLogger(typeof(TppAis));

        private readonly InputBufferQueue<QueuedInput> _inputBufferQueue;

        private readonly Chat _chat;

        private TppAis()
        {
            _inputBufferQueue = new InputBufferQueue<QueuedInput>(/* timings can be customized*/);

            var rootConfig = LoadConfig();

            IUserRepo dummyUserRepo = new DummyUserRepo();

            var inputParser = InputParserBuilder.FromBare()
                .Buttons("a", "b", "start", "select", "wait")
                .StartSelectConflict()
                .DPad(prefix: "")
                .RemappedDPad(up: "n", down: "s", left: "w", right: "e", mapsToPrefix: "")
                .AliasedButtons(("p", "wait"), ("xp", "wait"), ("exp", "wait"))
                .LengthRestrictions(maxSetLength: 2, maxSequenceLength: 1)
                .Build();

            _chat = new Chat(
                ircConfig: rootConfig.Irc,
                userRepo: dummyUserRepo,
                inputParserProvider: () => inputParser);
            _chat.OnMessage += ChatOnMessage;
        }

        private static void SetUpLoggingConfig()
        {
            var assembly = Assembly.GetEntryAssembly();
            var logRepository = LogManager.GetRepository(assembly);
            XmlConfigurator.Configure(logRepository,
                assembly?.GetManifestResourceStream("TppAis.resources.log4net.config"));
        }

        private static RootConfig LoadConfig()
        {
            var resolver = new PrivateSetterContractResolver();
            string configStr = File.ReadAllText("config.json");
            return JsonConvert.DeserializeObject<RootConfig>(configStr, new JsonSerializerSettings
            {
                ContractResolver = resolver,
                MissingMemberHandling = MissingMemberHandling.Error,
                // if we don't force objects to be fully replaced, it might e.g. append to default collections,
                // making it impossible to remove elements from said collections via configuration.
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            })!;
        }

        private async Task Run()
        {
            _chat.Start();
            await RunWebServer();
            _chat.Stop();
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        private async Task RunWebServer()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                // var request = context.Request;
                var response = context.Response;

                (var queuedInput, float duration) = await _inputBufferQueue.DequeueWaitAsync();
                var user = queuedInput.User;
                var inputSet = queuedInput.InputSet;

                var inputMap = new Dictionary<string, object>();
                foreach (var input in inputSet.Inputs)
                {
                    inputMap[input.EffectiveText] = input.AdditionalData;
                }

                response.ContentType = MediaTypeNames.Application.Json;
                var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                {
                    Duration = duration,
                    User = user,
                    InputMap = inputMap,
                }, settings: SerializerSettings, formatting: Formatting.Indented));
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            listener.Stop();
        }

        private void ChatOnMessage(object sender, Message message)
        {
            if (message.InputSequence.HasValue)
            {
                var inputSet = message.InputSequence.Value.InputSets.First();
                _inputBufferQueue.Enqueue(new QueuedInput() {InputSet = inputSet, User = message.User});
                Log.Info($"INPUT {message.User.TwitchDisplayName}: {message.MessageText}");
            }
            else
            {
                Log.Info($"CHAT {message.User.TwitchDisplayName}: {message.MessageText}");
            }
        }
    }
}
