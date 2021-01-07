using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TPP.Inputting
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

        private readonly float _bufferLengthSeconds;
        private readonly float _speedupRate;
        private readonly float _slowdownRate;
        private readonly float _minInputDuration;
        private readonly float _maxInputDuration;
        private readonly int _maxBufferLength;

        private float _prevInputDuration;

        private readonly Queue<TaskCompletionSource<(T, float)>> _awaitedDequeueings = new();

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
        /// <param name="maxBufferLength">Maximum number of queued inputs before new incoming inputs are discarded.
        /// Prevents the queue from growing unbounded if inputs aren't dequeued fast enough for some reason.</param>
        public InputBufferQueue(
            float bufferLengthSeconds = 3f,
            float speedupRate = 0.2f,
            float slowdownRate = 1f,
            float minInputDuration = 1 / 60f,
            float maxInputDuration = 100 / 60f,
            int maxBufferLength = 1000)
        {
            _minInputDuration = minInputDuration;
            _bufferLengthSeconds = bufferLengthSeconds;
            _speedupRate = speedupRate;
            _slowdownRate = slowdownRate;
            _minInputDuration = minInputDuration;
            _maxInputDuration = maxInputDuration;
            _prevInputDuration = _bufferLengthSeconds;
            _maxBufferLength = maxBufferLength;
        }

        private float CalcInputDuration(float prevInputDuration)
        {
            // if the target duration is n seconds and we have m inputs, each input has a duration of n/m
            float inputDuration = _bufferLengthSeconds / Math.Max(_queue.Count, 1);
            float delta = inputDuration - prevInputDuration;
            // smoothing. if delta > 0, time per input increased and therefore overall speed dropped
            float rate = delta > 0 ? _slowdownRate : _speedupRate;
            inputDuration = prevInputDuration + delta * rate;
            // clamp to min/max
            inputDuration = Math.Clamp(inputDuration, min: _minInputDuration, max: _maxInputDuration);
            return inputDuration;
        }

        /// <summary>
        /// Enqueue a new input.
        /// </summary>
        /// <param name="value">The input to enqueue.</param>
        /// <returns>If the input was enqueued. False if e.g. the queue is at maximum capacity.</returns>
        public bool Enqueue(T value)
        {
            if (_queue.Count >= _maxBufferLength) return false;
            _queue.Enqueue(value);
            if (_awaitedDequeueings.TryDequeue(out TaskCompletionSource<(T, float)>? task))
            {
                task.SetResult(Dequeue());
            }
            return true;
        }

        /// <summary>
        /// Dequeue the next input.
        /// </summary>
        /// <returns>a (input, duration) tuple for the next input. The duration is in seconds</returns>
        public (T, float) Dequeue()
        {
            float inputDuration = CalcInputDuration(_prevInputDuration);
            _prevInputDuration = inputDuration;
            (T, float inputDuration) result = (_queue.Dequeue(), inputDuration);
            return result;
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
                _awaitedDequeueings.Enqueue(taskCompletionSource);
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
            while (_awaitedDequeueings.Any())
            {
                _awaitedDequeueings.Dequeue().SetCanceled();
            }
        }

        public bool IsEmpty => !_queue.Any();
    }
}
