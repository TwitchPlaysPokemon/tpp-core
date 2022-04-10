using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Inputting;
using TPP.Inputting.Inputs;
using TPP.Inputting.Parsing;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Modes
{
    public sealed class Runmode : IMode, IDisposable
    {
        private readonly ILogger<Runmode> _logger;

        private readonly IUserRepo _userRepo;
        private readonly IRunCounterRepo _runCounterRepo;
        private readonly IInputLogRepo _inputLogRepo;
        private readonly IInputSidePicksRepo _inputSidePicksRepo;
        private IInputParser _inputParser;
        private readonly InputServer _inputServer;
        private readonly WebsocketBroadcastServer _broadcastServer;
        private AnarchyInputFeed _anarchyInputFeed;
        private readonly OverlayConnection _overlayConnection;

        private readonly StopToken _stopToken;
        private readonly MuteInputsToken _muteInputsToken;
        private readonly ModeBase _modeBase;
        private readonly int? _runNumber;

        public Runmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, Func<RunmodeConfig> configLoader)
        {
            RunmodeConfig runmodeConfig = configLoader();
            _logger = loggerFactory.CreateLogger<Runmode>();
            _stopToken = new StopToken();
            _muteInputsToken = new MuteInputsToken { Muted = runmodeConfig.MuteInputsAtStartup };
            Setups.Databases repos = Setups.SetUpRepositories(_logger, baseConfig);
            _userRepo = repos.UserRepo;
            _runCounterRepo = repos.RunCounterRepo;
            _inputLogRepo = repos.InputLogRepo;
            _inputSidePicksRepo = repos.InputSidePicksRepo;
            _runNumber = runmodeConfig.RunNumber;
            (_broadcastServer, _overlayConnection) = Setups.SetUpOverlayServer(loggerFactory,
                baseConfig.OverlayWebsocketHost, baseConfig.OverlayWebsocketPort);
            _modeBase = new ModeBase(
                loggerFactory, repos, baseConfig, _stopToken, _muteInputsToken, _overlayConnection, ProcessMessage);
            _modeBase.InstallAdditionalCommand(new Command("reloadinputconfig", _ =>
            {
                (_inputParser, _anarchyInputFeed) = ConfigToInputStuff(configLoader().InputConfig);
                return Task.FromResult(new CommandResult { Response = "input config reloaded" });
            }));

            // TODO felk: this feels a bit messy the way it is done right now,
            //            but I am unsure yet how I'd integrate the individual parts in a cleaner way.
            (_inputParser, _anarchyInputFeed) = ConfigToInputStuff(runmodeConfig.InputConfig);
            _inputServer = new InputServer(loggerFactory.CreateLogger<InputServer>(),
                runmodeConfig.InputServerHost, runmodeConfig.InputServerPort,
                _muteInputsToken, () => _anarchyInputFeed);
        }

        private AnarchyInputFeed CreateInputFeedFromConfig(InputConfig config, InputBufferQueue<QueuedInput> inputBufferQueue)
        {
            IInputMapper inputMapper = CreateInputMapperFromConfig(config);
            IInputHoldTiming inputHoldTiming = CreateInputHoldTimingFromConfig(config);

            return new AnarchyInputFeed(
                _overlayConnection,
                inputHoldTiming,
                inputMapper,
                inputBufferQueue,
                config.FramesPerSecond);
        }

        private IInputMapper CreateInputMapperFromConfig(InputConfig config) =>
            new DefaultTppInputMapper(config.FramesPerSecond, _muteInputsToken);

        private static IInputHoldTiming CreateInputHoldTimingFromConfig(InputConfig config) =>
            new DefaultInputHoldTiming(
                minSleepDuration: config.MinSleepFrames / (float)config.FramesPerSecond,
                minPressDuration: config.MinPressFrames / (float)config.FramesPerSecond,
                maxPressDuration: config.MaxPressFrames / (float)config.FramesPerSecond,
                maxHoldDuration: config.MaxHoldFrames / (float)config.FramesPerSecond);

        private static InputBufferQueue<QueuedInput>.Config CreateBufferConfig(InputConfig config) =>
            new(BufferLengthSeconds: config.BufferLengthSeconds,
                SpeedupRate: config.SpeedupRate,
                SlowdownRate: config.SlowdownRate,
                MinInputDuration: config.MinInputFrames / (float)config.FramesPerSecond,
                MaxInputDuration: config.MaxInputFrames / (float)config.FramesPerSecond,
                MaxBufferLength: config.MaxBufferLength);

        private (IInputParser, AnarchyInputFeed) ConfigToInputStuff(InputConfig config)
        {
            // TODO endpoints to control configs at runtime?
            IInputParser inputParser = config.ButtonsProfile.ToInputParserBuilder().Build();
            if (inputParser is SidedInputParser sidedInputParser)
                sidedInputParser.AllowDirectedInputs = config.AllowDirectedInputs;
            var inputBufferQueue = new InputBufferQueue<QueuedInput>(CreateBufferConfig(config));
            AnarchyInputFeed anarchyInputFeed = CreateInputFeedFromConfig(config, inputBufferQueue);
            return (inputParser, anarchyInputFeed);
        }

        // TODO It feels a bit dirty having this very specific use case bubble all the way up here.
        private async Task ProcessPotentialSidedInputs(User user, InputSequence inputSequence)
        {
            foreach (InputSet inputSet in inputSequence.InputSets)
            {
                if (inputSet.Inputs.FirstOrDefault(i => i is SideInput) is SideInput { Side: null } sideInput)
                {
                    string? side = await _inputSidePicksRepo.GetSide(user.Id);
                    sideInput.Side = side switch
                    {
                        "left" => InputSide.Left,
                        "right" => InputSide.Right,
                        _ => null
                    };
                }
            }
        }

        private async Task<bool> ProcessMessage(Message message)
        {
            if (message.MessageSource != MessageSource.Chat) return false;
            string potentialInput = message.MessageText.Split(' ', count: 2)[0];
            InputSequence? input = _inputParser.Parse(potentialInput);
            if (input == null) return false;
            await ProcessPotentialSidedInputs(message.User, input);
            foreach (InputSet inputSet in input.InputSets)
                await _anarchyInputFeed.Enqueue(inputSet, message.User);
            if (!_muteInputsToken.Muted)
                await CollectRunStatistics(message.User, input, rawInput: potentialInput);
            return true;
        }

        private async Task CollectRunStatistics(User user, InputSequence input, string rawInput)
        {
            await _inputLogRepo.LogInput(user.Id, rawInput, SystemClock.Instance.GetCurrentInstant());
            if (_runNumber != null && !user.ParticipationEmblems.Contains(_runNumber.Value))
                await _userRepo.GiveEmblem(user, _runNumber.Value);
            long counter = await _runCounterRepo.Increment(_runNumber, incrementBy: input.InputSets.Count);
            await _overlayConnection.Send(new ButtonPressUpdate(counter), CancellationToken.None);
        }

        public async Task Run()
        {
            _logger.LogInformation("Runmode starting");
            Task overlayWebsocketTask = _broadcastServer.Listen();
            Task inputServerTask = _inputServer.Listen();
            _modeBase.Start();
            while (!_stopToken.ShouldStop)
            {
                // TODO run main loop goes here
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            _inputServer.Stop();
            await inputServerTask;
            await _broadcastServer.Stop();
            await overlayWebsocketTask;
            _logger.LogInformation("Runmode ended");
        }

        public void Cancel()
        {
            // once the mainloop is not just busylooping, this needs to be replaced with something
            // that makes the mode stop immediately
            _stopToken.ShouldStop = true;
        }

        public void Dispose()
        {
            _modeBase.Dispose();
        }
    }
}
