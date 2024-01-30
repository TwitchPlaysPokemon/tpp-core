using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Chat;
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

namespace TPP.Core.Modes;

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
    private RunmodeConfig _runmodeConfig;

    public Runmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, Func<RunmodeConfig> configLoader)
    {
        _runmodeConfig = configLoader();
        _logger = loggerFactory.CreateLogger<Runmode>();
        _stopToken = new StopToken();
        _muteInputsToken = new MuteInputsToken { Muted = _runmodeConfig.MuteInputsAtStartup };
        Setups.Databases repos = Setups.SetUpRepositories(loggerFactory, _logger, baseConfig);
        _userRepo = repos.UserRepo;
        _runCounterRepo = repos.RunCounterRepo;
        _inputLogRepo = repos.InputLogRepo;
        _inputSidePicksRepo = repos.InputSidePicksRepo;
        (_broadcastServer, _overlayConnection) = Setups.SetUpOverlayServer(loggerFactory,
            baseConfig.OverlayWebsocketHost, baseConfig.OverlayWebsocketPort);
        _modeBase = new ModeBase(
            loggerFactory, repos, baseConfig, _stopToken, _muteInputsToken, _overlayConnection, ProcessMessage);
        _modeBase.InstallAdditionalCommand(new Command("reloadrunconfig", _ =>
        {
            RunmodeConfig config = configLoader();
            (_inputParser, _anarchyInputFeed) = ConfigToInputStuff(config.InputConfig);
            _runmodeConfig = config;
            return Task.FromResult(new CommandResult { Response = "input config reloaded" });
        }));

        // Only install dual run commands if we're in dual run mode
        if (_runmodeConfig.InputConfig.ButtonsProfile.ToInputParserBuilder().IsDualRun)
        {
            var dualRunCommands = new DualRunCommands(
                repos.InputSidePicksRepo, SystemClock.Instance, () => _runmodeConfig.SwitchSidesCooldown);
            foreach (Command command in dualRunCommands.Commands)
                _modeBase.InstallAdditionalCommand(command);
        }

        // TODO felk: this feels a bit messy the way it is done right now,
        //            but I am unsure yet how I'd integrate the individual parts in a cleaner way.
        (_inputParser, _anarchyInputFeed) = ConfigToInputStuff(_runmodeConfig.InputConfig);
        _inputServer = new InputServer(loggerFactory.CreateLogger<InputServer>(),
            _runmodeConfig.InputServerHost, _runmodeConfig.InputServerPort,
            _muteInputsToken, () => _anarchyInputFeed);
    }

    private AnarchyInputFeed CreateInputFeedFromConfig(InputConfig config,
        InputBufferQueue<QueuedInput> inputBufferQueue)
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
        new DefaultTppInputMapper(config.FramesPerSecond, _muteInputsToken, config.ControllerPrefix, config.ControllerPrefix2);

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
        var inputParserBuilder = config.ButtonsProfile.ToInputParserBuilder().HoldEnabled(config.AllowHeldInputs);
        if (config.MaxSetLength > 0)
            inputParserBuilder.MaxSetLength(config.MaxSetLength);
        if (config.MaxSequenceLength > 0)
            inputParserBuilder.MaxSequenceLength(config.MaxSequenceLength);

        IInputParser inputParser = inputParserBuilder.Build();
        if (inputParser is SidedInputParser sidedInputParser)
            sidedInputParser.AllowDirectedInputs = config.AllowDirectedInputs;
        var inputBufferQueue = new InputBufferQueue<QueuedInput>(CreateBufferConfig(config));
        AnarchyInputFeed anarchyInputFeed = CreateInputFeedFromConfig(config, inputBufferQueue);
        return (inputParser, anarchyInputFeed);
    }

    // TODO It feels a bit dirty having this very specific use case bubble all the way up here.
    private static bool _sideFlipFlop;
    private async Task ProcessPotentialSidedInputs(IChat chat, Message message, InputSequence inputSequence)
    {
        foreach (InputSet inputSet in inputSequence.InputSets)
        {
            if (inputSet.Inputs.FirstOrDefault(i => i is SideInput) is SideInput { Side: null } sideInput)
            {
                string side;
                SidePick? sidePick = await _inputSidePicksRepo.GetSidePick(message.User.Id);
                if (sidePick?.Side == null)
                {
                    side = _sideFlipFlop ? "left" : "right";
                    if (_runmodeConfig.AutoAssignSide)
                    {
                        // New users might input a plain "left" or "right" instead of properly picking a side.
                        // It may confuse them if they get assigned to the side named opposite of their input,
                        // so let's use that directional input as their side pick instead of flip-flopping.
                        if (inputSet.Inputs.Any(i => i.OriginalText.ToLowerInvariant() == "left"))
                            side = "left";
                        else if (inputSet.Inputs.Any(i => i.OriginalText.ToLowerInvariant() == "right"))
                            side = "right";
                        await _inputSidePicksRepo.SetSide(message.User.Id, side);
                        await chat.SendMessage(
                            $"you were auto-assigned to the {side} side team. " +
                            "You can change your team with !left or !right", responseTo: message);
                    }
                    _sideFlipFlop = !_sideFlipFlop;
                }
                else
                {
                    side = sidePick.Side;
                }
                sideInput.Side = side switch
                {
                    "left" => InputSide.Left,
                    "right" => InputSide.Right,
                    _ => null
                };
            }
        }
    }

    private async Task<bool> ProcessMessage(IChat chat, Message message)
    {
        if (message.MessageSource != MessageSource.Chat) return false;
        string potentialInput = message.MessageText.Split(' ', count: 2)[0];
        InputSequence? input = _inputParser.Parse(potentialInput);
        if (input == null) return false;
        await ProcessPotentialSidedInputs(chat, message, input);
        foreach (InputSet inputSet in input.InputSets)
            await _anarchyInputFeed.Enqueue(inputSet, message.User);
        if (!_muteInputsToken.Muted)
            await CollectRunStatistics(message.User, input, rawInput: potentialInput);
        return true;
    }

    private async Task CollectRunStatistics(User user, InputSequence input, string rawInput)
    {
        await _inputLogRepo.LogInput(user.Id, rawInput, SystemClock.Instance.GetCurrentInstant());
        int? runNumber = _runmodeConfig.RunNumber;
        if (runNumber != null && !user.ParticipationEmblems.Contains(runNumber.Value))
            await _userRepo.GiveEmblem(user, runNumber.Value);
        long counter = await _runCounterRepo.Increment(runNumber, incrementBy: input.InputSets.Count);
        await _overlayConnection.Send(new ButtonPressUpdate(counter), CancellationToken.None);
    }

    public async Task Run()
    {
        _logger.LogInformation("Runmode starting");
        Task overlayWebsocketTask = _broadcastServer.Listen();
        Task inputServerTask = _inputServer.Listen();
        _modeBase.Start();
        Task handleStopTask = Task.Run(async () =>
        {
            while (!_stopToken.ShouldStop)
                // Just wait until it is time to shut everything down
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            _inputServer.Stop();
            await _broadcastServer.Stop();
        });
        // Must wait on all concurrently running tasks simultaneously to know when one of them crashed
        await Task.WhenAll(handleStopTask, inputServerTask, overlayWebsocketTask);
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
