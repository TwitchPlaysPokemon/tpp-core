using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inputting
{
    /// <summary>
    /// The input buffer queue is responsible for putting inputs in reference to time,
    /// meaning when and for how long each input should be executed.
    /// It allows for inputs to be queued and dequeued,
    /// but dequeuing an input also returns a duration for that input.
    /// </summary>
    public class InputBufferQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(initialCount: 1);

        private readonly float _bufferLengthSeconds;
        private readonly float _speedupRate;
        private readonly float _slowdownRate;
        private readonly float _minInputDuration;
        private readonly float _maxInputDuration;

        private float _prevInputDuration;

        private readonly Queue<TaskCompletionSource<(T, float)>> _awaitedDequeuings =
            new Queue<TaskCompletionSource<(T, float)>>();

        /// <summary>
        /// Create a new buffer for the given settings.
        /// </summary>
        /// <param name="bufferLengthSeconds">Duration in seconds the buffer queue aims to buffer inputs for.
        /// E.g. if it is set to 3 seconds and inputs get enqueued and dequeued at a constant rate of 5 per second,
        /// the queue will stabilize at 15 items.</param>
        /// <param name="speedupRate">The rate at which input speed can increase,
        /// as a percentage from 0 to 1 with 1 being instantaneous.</param>
        /// <param name="slowdownRate">The rate at which input speed can decrease,
        /// as a percentage from 0 to 1 with 1 being instantaneous.</param>
        /// <param name="minInputDuration">The minimum duration a single input can have, in seconds.</param>
        /// <param name="maxInputDuration">The maximum duration a single input can have, in seconds.</param>
        public InputBufferQueue(
            float bufferLengthSeconds = 3f,
            float speedupRate = 0.2f,
            float slowdownRate = 1f,
            float minInputDuration = 1 / 60f,
            float maxInputDuration = 100 / 60f)
        {
            _minInputDuration = minInputDuration;
            _bufferLengthSeconds = bufferLengthSeconds;
            _speedupRate = speedupRate;
            _slowdownRate = slowdownRate;
            _minInputDuration = minInputDuration;
            _maxInputDuration = maxInputDuration;
            _prevInputDuration = _bufferLengthSeconds;
        }

        private float CalcInputDuration(float prevInputDuration)
        {
            // if the target duration is n seconds and we have m inputs, each input has a duration of n/m.
            // Also treat an empty queue as length 1, so Enqueue() can directly serve any awaited dequeueings
            // without having to queue the item just to immediately dequeuing it again.
            float inputDuration = _bufferLengthSeconds / Math.Max(_queue.Count, 1);
            float delta = inputDuration - prevInputDuration;
            // smoothing. if delta > 0, time per input increased and therefore overall speed dropped
            float rate = delta > 0 ? _slowdownRate : _speedupRate;
            inputDuration = prevInputDuration + delta * rate;
            // clamp to min/max
            inputDuration = Math.Clamp(inputDuration, min: _minInputDuration, max: _maxInputDuration);
            return inputDuration;
        }

        private float NextInputDuration()
        {
            _prevInputDuration = CalcInputDuration(_prevInputDuration);
            return _prevInputDuration;
        }

        /// <summary>
        /// Enqueue a new input.
        /// </summary>
        /// <param name="value">The input to enqueue.</param>
        public void Enqueue(T value)
        {
            _semaphore.Wait();
            if (_awaitedDequeuings.TryDequeue(out var task))
            {
                task.SetResult((value, NextInputDuration()));
            }
            else
            {
                _queue.Enqueue(value);
            }
            _semaphore.Release();
        }

        /// <summary>
        /// Dequeue the next input.
        /// </summary>
        /// <returns>a (input, duration) tuple for the next input. The duration is in seconds</returns>
        public (T, float) Dequeue()
        {
            float duration = NextInputDuration(); // calculate before dequeuing
            return (_queue.Dequeue(), duration);
        }

        /// <summary>
        /// Dequeue the next input asynchronously, waiting for the next input if the queue is currently empty.
        /// </summary>
        /// <returns>a task containing a (input, duration) tuple for the next input. The duration is in seconds</returns>
        public async Task<(T, float)> DequeueWaitAsync()
        {
            if (IsEmpty)
            {
                var taskCompletionSource = new TaskCompletionSource<(T, float)>();
                _awaitedDequeuings.Enqueue(taskCompletionSource);
                return await taskCompletionSource.Task;
            }
            else
            {
                return await Task.FromResult(Dequeue());
            }
        }

        /// <summary>
        /// Clear the buffer queue.
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
            _prevInputDuration = _bufferLengthSeconds;
            while (_awaitedDequeuings.Any())
            {
                _awaitedDequeuings.Dequeue().SetCanceled();
            }
        }

        public bool IsEmpty => !_queue.Any();
    }
}
