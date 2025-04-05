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

public sealed class AnarchyInputFeed(
    OverlayConnection overlayConnection,
    IInputHoldTiming inputHoldTiming,
    IInputMapper inputMapper,
    InputBufferQueue<QueuedInput> inputBufferQueue,
    float fps,
    QueueTransitionInputs queueTransitionInputs)
    : IInputFeed
{
    // TODO: is it okay this triggers overlay events by itself? Maybe use events and hook up from the outside.

    private static long _prevInputId = 0;
    private InputMap? _activeInput = null;
    private bool _wasQueueEmptyLastPoll = true; // treat a fresh start as "paused"

    public async Task Enqueue(InputSet inputSet, User user, string? channel, string? channelImageUrl)
    {
        long inputId = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
        if (inputId <= _prevInputId) inputId = _prevInputId + 1;
        _prevInputId = inputId;
        QueuedInput queuedInput = new(inputId, inputSet);
        bool enqueued = inputBufferQueue.Enqueue(queuedInput);
        if (enqueued)
            await overlayConnection.Send(
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
    private const float PauseInputDuration = 2 / 60f;
    private const float UnpauseInputDuration = 2 / 60f;

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

        InputMap inputMap;
        bool queueEmpty = inputBufferQueue.IsEmpty;
        if (queueEmpty)
        {
            if (!_wasQueueEmptyLastPoll && queueTransitionInputs.QueueEmpty != null)
            {
                inputMap = inputMapper.Map(inputHoldTiming.TimeInput(EmptyInputSet, PauseInputDuration));
                inputMap[queueTransitionInputs.QueueEmpty] = true;
                _activeInput = inputMap;
            }
            else
            {
                inputMap = new Dictionary<string, object>();
                // empty input map doesn't need to be stored as the active input
            }
        }
        else
        {
            if (_wasQueueEmptyLastPoll && queueTransitionInputs.QueueNoLongerEmpty != null)
            {
                inputMap = inputMapper.Map(inputHoldTiming.TimeInput(EmptyInputSet, UnpauseInputDuration));
                inputMap[queueTransitionInputs.QueueNoLongerEmpty] = true;
                _activeInput = inputMap;
            }
            else
            {
                (QueuedInput queuedInput, float duration) = inputBufferQueue.Dequeue();
                duration = (float)(Math.Round(duration * 60f) / 60f);
                TimedInputSet timedInputSet = inputHoldTiming.TimeInput(queuedInput.InputSet, duration);
                inputMap = inputMapper.Map(timedInputSet);
                inputMap["Input_Id"] = queuedInput.InputId; // So client can reject duplicate inputs

                await overlayConnection.Send(new AnarchyInputStart(queuedInput.InputId, timedInputSet, fps),
                    CancellationToken.None);
                Task _ = Task.Delay(TimeSpan.FromSeconds(timedInputSet.HoldDuration))
                    .ContinueWith(async _ => await overlayConnection.Send(
                        new AnarchyInputStop(queuedInput.InputId), CancellationToken.None));
                _activeInput = inputMap;
            }
        }
        _wasQueueEmptyLastPoll = queueEmpty;

        return getResultForEndpoint(inputMap);
    }
}
