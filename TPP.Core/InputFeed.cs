using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Inputting;
using TPP.Inputting.Inputs;
using TPP.Model;
using InputMap = System.Collections.Generic.IDictionary<string, object>;

namespace TPP.Core;

public record QueuedInput(long InputId, InputSet InputSet);

/// <summary>
/// TODO: I am unsure if this structure will work for all the stuff not implemented yet (e.g. democracy, demohouses)
///       or if another structure will be better.
/// </summary>
public interface IInputFeed
{
    public Task<InputMap?> HandleRequest(string? path);
}

public record QueueTransitionInputs(string? QueueEmpty, string? QueueNoLongerEmpty);

public sealed class AnarchyInputFeed : IInputFeed
{
    // TODO: is it okay this triggers overlay events by itself? Maybe use events and hook up from the outside.
    private readonly OverlayConnection _overlayConnection;
    private readonly IInputHoldTiming _inputHoldTiming;
    private readonly IInputMapper _inputMapper;
    private readonly InputBufferQueue<QueuedInput> _inputBufferQueue;
    private readonly float _fps;

    private static long _prevInputId = 0;
    private InputMap? _activeInput = null;
    private bool _wasQueueEmptyLastPoll = true; // treat a fresh start as "paused"
    private readonly QueueTransitionInputs _queueTransitionInputs;

    public AnarchyInputFeed(
        OverlayConnection overlayConnection,
        IInputHoldTiming inputHoldTiming,
        IInputMapper inputMapper,
        InputBufferQueue<QueuedInput> inputBufferQueue,
        float fps,
        QueueTransitionInputs queueTransitionInputs)
    {
        _overlayConnection = overlayConnection;
        _inputHoldTiming = inputHoldTiming;
        _inputMapper = inputMapper;
        _inputBufferQueue = inputBufferQueue;
        _fps = fps;
        _queueTransitionInputs = queueTransitionInputs;
    }

    public async Task Enqueue(InputSet inputSet, User user, string? channel, string? channelImageUrl)
    {
        long inputId = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
        if (inputId <= _prevInputId) inputId = _prevInputId + 1;
        _prevInputId = inputId;
        QueuedInput queuedInput = new(inputId, inputSet);
        bool enqueued = _inputBufferQueue.Enqueue(queuedInput);
        if (enqueued)
            await _overlayConnection.Send(
                new NewAnarchyInput(queuedInput.InputId, queuedInput.InputSet, user, channel, channelImageUrl),
                CancellationToken.None);
    }

    private static string CasefoldKeyForDesmume(string key) =>
        key.Length == 1 ? key : key.ToLower();

    private static readonly Dictionary<string, Func<InputMap, InputMap>> Endpoints = new()
    {
        ["/gbmode_input_request_bizhawk"] = inputMap => inputMap,
        ["/gbmode_input_request_desmume"] = inputMap => inputMap
            .ToDictionary(kvp => CasefoldKeyForDesmume(kvp.Key), kvp => kvp.Value),
    };

    private static readonly InputSet EmptyInputSet = new(ImmutableList<Input>.Empty);
    private const float EmptyInputDuration = 6 / 60f;

    public async Task<InputMap?> HandleRequest(string? path)
    {
        if (path == "/gbmode_input_complete")
        {
            _activeInput = null;
            return null;
        }
        if (!Endpoints.TryGetValue(path ?? "", out Func<InputMap, InputMap>? getResultForEndpoint))
            throw new ArgumentException($"unrecognized input polling endpoint '{path}'. " +
                                        $"Available endpoints: {string.Join(", ", Endpoints.Keys)}");
        if (_activeInput != null)
            return getResultForEndpoint(_activeInput);

        bool queueEmpty;
        InputMap inputMap;
        if (_inputBufferQueue.IsEmpty)
        {
            queueEmpty = true;
            TimedInputSet timedInputSet = _inputHoldTiming.TimeInput(EmptyInputSet, EmptyInputDuration);
            inputMap = _inputMapper.Map(timedInputSet);
        }
        else
        {
            queueEmpty = false;
            (QueuedInput queuedInput, float duration) = _inputBufferQueue.Dequeue();
            duration = (float)(Math.Round(duration * 60f) / 60f);
            TimedInputSet timedInputSet = _inputHoldTiming.TimeInput(queuedInput.InputSet, duration);
            inputMap = _inputMapper.Map(timedInputSet);
            inputMap["Input_Id"] = queuedInput.InputId; // So client can reject duplicate inputs

            await _overlayConnection.Send(new AnarchyInputStart(queuedInput.InputId, timedInputSet, _fps),
                CancellationToken.None);
            Task _ = Task.Delay(TimeSpan.FromSeconds(timedInputSet.HoldDuration))
                .ContinueWith(async _ => await _overlayConnection.Send(
                    new AnarchyInputStop(queuedInput.InputId), CancellationToken.None));
        }
        if (!_wasQueueEmptyLastPoll && queueEmpty && _queueTransitionInputs.QueueEmpty != null)
            inputMap[_queueTransitionInputs.QueueEmpty] = true;
        if (_wasQueueEmptyLastPoll && !queueEmpty && _queueTransitionInputs.QueueNoLongerEmpty != null)
            inputMap[_queueTransitionInputs.QueueNoLongerEmpty] = true;
        _wasQueueEmptyLastPoll = queueEmpty;

        _activeInput = inputMap;

        return getResultForEndpoint(inputMap);
    }
}
