using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TPP.Inputting;

/// <summary>
/// The input buffer queue is responsible for putting inputs in reference to time,
/// meaning when and for how long each input should be executed.
/// It allows for inputs to be queued and dequeued,
/// but dequeuing an input also returns a duration for that input.
/// </summary>
public class InputBufferQueue<T>
{
    /// <summary>
    /// The queue's configuration.
    /// </summary>
    /// <param name="BufferLengthSeconds">Duration in seconds the buffer queue aims to buffer inputs for.
    /// E.g. if it is set to 3 seconds and inputs get enqueued and dequeued at a constant rate of 5 per second,
    /// the queue will stabilize at 15 items.</param>
    /// <param name="SpeedupRate">The rate at which input speed can increase,
    /// as a percentage from 0 to 1 with 1 being instantaneous.</param>
    /// <param name="SlowdownRate">The rate at which input speed can decrease,
    /// as a percentage from 0 to 1 with 1 being instantaneous.</param>
    /// <param name="MinInputDuration">The minimum duration a single input can have, in seconds.</param>
    /// <param name="MaxInputDuration">The maximum duration a single input can have, in seconds.</param>
    /// <param name="MaxBufferLength">Maximum number of queued inputs before new incoming inputs are discarded.
    /// Prevents the queue from growing unbounded if inputs aren't dequeued fast enough for some reason.</param>
    public sealed record Config(
        float BufferLengthSeconds = 3f,
        float SpeedupRate = 0.2f,
        float SlowdownRate = 1f,
        float MinInputDuration = 1 / 60f,
        float MaxInputDuration = 100 / 60f,
        int MaxBufferLength = 1000);

    private readonly Queue<T> _queue = new();
    private readonly Queue<TaskCompletionSource<(T, float)>> _awaitedDequeueings = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1);

    private Config _config;
    private float _prevInputDuration;

    public InputBufferQueue(Config? config = null)
    {
        _config = config ?? new Config();
        _prevInputDuration = _config.BufferLengthSeconds;
    }

    public void SetNewConfig(Config config)
    {
        _config = config;
    }

    private float CalcInputDuration(float prevInputDuration)
    {
        // if the target duration is n seconds and we have m inputs, each input has a duration of n/m
        float inputDuration = _config.BufferLengthSeconds / Math.Max(_queue.Count, 1);
        float delta = inputDuration - prevInputDuration;
        // smoothing. if delta > 0, time per input increased and therefore overall speed dropped
        float rate = delta > 0 ? _config.SlowdownRate : _config.SpeedupRate;
        inputDuration = prevInputDuration + delta * rate;
        // clamp to min/max
        inputDuration = Math.Clamp(inputDuration, min: _config.MinInputDuration, max: _config.MaxInputDuration);
        return inputDuration;
    }

    /// <summary>
    /// Enqueue a new input.
    /// </summary>
    /// <param name="value">The input to enqueue.</param>
    /// <returns>If the input was enqueued. False if e.g. the queue is at maximum capacity.</returns>
    public bool Enqueue(T value)
    {
        _semaphoreSlim.Wait();
        try
        {
            if (_queue.Count >= _config.MaxBufferLength) return false;
            _queue.Enqueue(value);
            if (_awaitedDequeueings.TryDequeue(out TaskCompletionSource<(T, float)>? task))
            {
                task.SetResult(Dequeue());
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
        return true;
    }

    /// <summary>
    /// Dequeue the next input.
    /// </summary>
    /// <returns>a (input, duration) tuple for the next input. The duration is in seconds</returns>
    public (T, float) Dequeue()
    {
        _semaphoreSlim.Wait();
        try
        {
            float inputDuration = CalcInputDuration(_prevInputDuration);
            _prevInputDuration = inputDuration;
            return (_queue.Dequeue(), inputDuration);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// Dequeue the next input asynchronously, waiting for the next input if the queue is currently empty.
    /// </summary>
    /// <returns>a task containing a (input, duration) tuple for the next input. The duration is in seconds</returns>
    public async Task<(T, float)> DequeueWaitAsync()
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (IsEmpty)
            {
                var taskCompletionSource = new TaskCompletionSource<(T, float)>();
                _awaitedDequeueings.Enqueue(taskCompletionSource);
                return await taskCompletionSource.Task;
            }
            else
            {
                return Dequeue();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    /// <summary>
    /// Clear the buffer queue.
    /// </summary>
    public void Clear()
    {
        _semaphoreSlim.Wait();
        try
        {
            _queue.Clear();
            _prevInputDuration = _config.BufferLengthSeconds;
            while (_awaitedDequeueings.Any())
            {
                _awaitedDequeueings.Dequeue().SetCanceled();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public bool IsEmpty => !_queue.Any();
}
