using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TPP.Core.Overlay;
using TPP.Core.Overlay.Events;
using TPP.Inputting;
using TPP.Model;
using InputMap = System.Collections.Generic.IDictionary<string, object>;

namespace TPP.Core;

public record QueuedInput(int InputId, InputSet InputSet);

/// <summary>
/// TODO: I am unsure if this structure will work for all the stuff not implemented yet (e.g. democracy, demohouses)
///       or if another structure will be better.
/// </summary>
public interface IInputFeed
{
    public Task<InputMap?> HandleRequest(string? path);
}

public sealed class AnarchyInputFeed : IInputFeed
{
    // TODO: is it okay this triggers overlay events by itself? Maybe use events and hook up from the outside.
    private readonly OverlayConnection _overlayConnection;
    private readonly IInputHoldTiming _inputHoldTiming;
    private readonly IInputMapper _inputMapper;
    private readonly InputBufferQueue<QueuedInput> _inputBufferQueue;
    private readonly float _fps;

    private static int _inputIdSeq = 1;

    public AnarchyInputFeed(
        OverlayConnection overlayConnection,
        IInputHoldTiming inputHoldTiming,
        IInputMapper inputMapper,
        InputBufferQueue<QueuedInput> inputBufferQueue,
        float fps)
    {
        _overlayConnection = overlayConnection;
        _inputHoldTiming = inputHoldTiming;
        _inputMapper = inputMapper;
        _inputBufferQueue = inputBufferQueue;
        _fps = fps;
    }

    public async Task Enqueue(InputSet inputSet, User user)
    {
        QueuedInput queuedInput = new(_inputIdSeq++, inputSet);
        bool enqueued = _inputBufferQueue.Enqueue(queuedInput);
        if (enqueued)
            await _overlayConnection.Send(new NewAnarchyInput(queuedInput.InputId, queuedInput.InputSet, user),
                CancellationToken.None);
    }

    public async Task<InputMap?> HandleRequest(string? path)
    {
        if (path == "/gbmode_input_complete")
        {
            return null;
        }

        static string CasefoldKeyForDesmume(string key) => key.Length == 1 ? key : key.ToLower();

        InputMap inputMap;
        if (_inputBufferQueue.IsEmpty)
        {
            inputMap = new Dictionary<string, object>();
        }
        else
        {
            (QueuedInput queuedInput, float duration) = _inputBufferQueue.Dequeue();
            duration = (float)(Math.Round(duration * 60f) / 60f);
            TimedInputSet timedInputSet = _inputHoldTiming.TimeInput(queuedInput.InputSet, duration);
            inputMap = _inputMapper.Map(timedInputSet);

            await _overlayConnection.Send(new AnarchyInputStart(queuedInput.InputId, timedInputSet, _fps),
                CancellationToken.None);
            Task _ = Task.Delay(TimeSpan.FromSeconds(timedInputSet.HoldDuration))
                .ContinueWith(async _ => await _overlayConnection.Send(
                    new AnarchyInputStop(queuedInput.InputId), CancellationToken.None));
        }
        return path switch
        {
            "/gbmode_input_request_bizhawk" => inputMap,
            "/gbmode_input_request_desmume" => inputMap
                .ToDictionary(kvp => CasefoldKeyForDesmume(kvp.Key), kvp => kvp.Value),
            _ => throw new ArgumentException("unrecognized input polling endpoint")
        };
    }
}
