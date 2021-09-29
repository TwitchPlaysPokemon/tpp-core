using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Commands;
using TPP.Core.Commands.Definitions;
using TPP.Core.Configuration;
using TPP.Core.Overlay;
using TPP.Inputting;
using TPP.Inputting.Parsing;

namespace TPP.Core.Modes;

public sealed class Runmode : IMode, IDisposable
{
    private readonly ILogger<Runmode> _logger;

    private IInputParser _inputParser;
    private readonly InputServer _inputServer;
    private readonly WebsocketBroadcastServer _broadcastServer;
    private AnarchyInputFeed _anarchyInputFeed;
    private readonly OverlayConnection _overlayConnection;
    private readonly InputBufferQueue<QueuedInput> _inputBufferQueue;

    private readonly StopToken _stopToken;
    private readonly ModeBase _modeBase;

    public Runmode(ILoggerFactory loggerFactory, BaseConfig baseConfig, Func<RunmodeConfig> configLoader)
    {
        RunmodeConfig runmodeConfig = configLoader();
        _logger = loggerFactory.CreateLogger<Runmode>();
        _stopToken = new StopToken();
        Setups.Databases repos = Setups.SetUpRepositories(_logger, baseConfig);
        (_broadcastServer, _overlayConnection) = Setups.SetUpOverlayServer(loggerFactory,
            baseConfig.OverlayWebsocketHost, baseConfig.OverlayWebsocketPort);
        _modeBase = new ModeBase(loggerFactory, repos, baseConfig, _stopToken, _overlayConnection, ProcessMessage);
        _modeBase.InstallAdditionalCommand(new Command("reloadinputconfig", _ =>
        {
            ReloadConfig(configLoader().InputConfig);
            return Task.FromResult(new CommandResult { Response = "input config reloaded" });
        }));

        // TODO felk: this feels a bit messy the way it is done right now,
        //            but I am unsure yet how I'd integrate the individual parts in a cleaner way.
        InputConfig inputConfig = runmodeConfig.InputConfig;
        _inputParser = inputConfig.ButtonsProfile.ToInputParser();
        _inputBufferQueue = new InputBufferQueue<QueuedInput>(CreateBufferConfig(inputConfig));
        _anarchyInputFeed = CreateInputFeedFromConfig(inputConfig);
        _inputServer = new InputServer(loggerFactory.CreateLogger<InputServer>(),
            runmodeConfig.InputServerHost, runmodeConfig.InputServerPort,
            _anarchyInputFeed);
    }

    private AnarchyInputFeed CreateInputFeedFromConfig(InputConfig config)
    {
        IInputMapper inputMapper = CreateInputMapperFromConfig(config);
        IInputHoldTiming inputHoldTiming = CreateInputHoldTimingFromConfig(config);
        _inputBufferQueue.SetNewConfig(CreateBufferConfig(config));

        return new AnarchyInputFeed(
            _overlayConnection,
            inputHoldTiming,
            inputMapper,
            _inputBufferQueue,
            config.FramesPerSecond);
    }

    private static IInputMapper CreateInputMapperFromConfig(InputConfig config) =>
        new DefaultTppInputMapper(config.FramesPerSecond);

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

    private void ReloadConfig(InputConfig config)
    {
        // TODO endpoints to control configs at runtime?
        _inputParser = config.ButtonsProfile.ToInputParser();
        _anarchyInputFeed = CreateInputFeedFromConfig(config);
        _inputServer.InputFeed = _anarchyInputFeed;
    }

    private async Task<bool> ProcessMessage(Message message)
    {
        if (message.MessageSource != MessageSource.Chat) return false;
        string potentialInput = message.MessageText.Split(' ', count: 2)[0];
        InputSequence? input = _inputParser.Parse(potentialInput);
        if (input == null) return false;
        foreach (InputSet inputSet in input.InputSets)
            await _anarchyInputFeed.Enqueue(inputSet, message.User);
        return true;
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
