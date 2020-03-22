using System;
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
using Models;
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

        private const float MaxPressDuration = 16 / 60f;
        private const float MinInputDuration = 1 / 60f;
        private const float EmptyQueueSleepDuration = 30 / 60f;

        private TppAis()
        {
            _inputBufferQueue = new InputBufferQueue<QueuedInput>( /* timings can be customized*/);

            var rootConfig = LoadConfig();

            IUserRepo dummyUserRepo = new DummyUserRepo();

            var inputParser = InputParserBuilder.FromBare()
                .AnalogStick(prefix: "l", allowSpin: true)
                .AnalogStick(prefix: "r", allowSpin: true)
                .RemappedAnalogStick("up", "down", "left", "right", "spinl", "spinr", mapsToPrefix: "l")
                .RemappedAnalogStick(up: "n", down: "s", left: "w", right: "e", spinl: null, spinr: null,
                    mapsToPrefix: "l")
                .RemappedAnalogStick(up: "ln", down: "ls", left: "lw", right: "le", spinl: null, spinr: null,
                    mapsToPrefix: "l")
                .RemappedAnalogStick(up: "rn", down: "rs", left: "rw", right: "re", spinl: null, spinr: null,
                    mapsToPrefix: "r")
                .Buttons("A", "B", "X", "Y", "L", "R", "zl", "zr", "plus", "minus", "lstick", "rstick")
                .Touchscreen(width: 1280, height: 720, multitouch: true, allowDrag: true)
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

        private async Task HandleRequest(HttpListenerContext context)
        {
            var response = context.Response;

            object data;
            if (_inputBufferQueue.IsEmpty)
            {
                // do not block. instead, send some sleep frames
                data = new
                {
                    DurationPress = 0f,
                    DurationSleep = EmptyQueueSleepDuration,
                    User = (User) null!,
                    InputMap = new Dictionary<string, object>(),
                };
            }
            else
            {
                (var queuedInput, float duration) = await _inputBufferQueue.DequeueWaitAsync();
                var user = queuedInput.User;
                var inputSet = queuedInput.InputSet;

                float durationPress;
                float durationSleep;
                if (inputSet.Inputs.Exists(i => i.EffectiveText == "hold"))
                {
                    durationPress = duration;
                    durationSleep = 0f;
                }
                else if (inputSet.Inputs.All(i => i.EffectiveText == "wait"))
                {
                    durationPress = 0f;
                    durationSleep = duration;
                }
                else
                {
                    float sleep = duration - MaxPressDuration;
                    if (sleep >= MinInputDuration)
                    {
                        durationPress = duration - sleep;
                        durationSleep = sleep;
                    }
                    else
                    {
                        durationPress = Math.Max(MinInputDuration, duration - MinInputDuration);
                        durationSleep = MinInputDuration;
                    }
                }

                var inputMap = new Dictionary<string, object>();
                foreach (var input in inputSet.Inputs)
                {
                    inputMap[input.EffectiveText] = input.AdditionalData;
                }
                data = new
                {
                    DurationPress = durationPress,
                    DurationSleep = durationSleep,
                    User = user,
                    InputMap = inputMap,
                };
            }

            response.ContentType = MediaTypeNames.Application.Json;
            var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                data, settings: SerializerSettings, formatting: Formatting.Indented));
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private async Task RunWebServer()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                await HandleRequest(context);
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
