using NUnit.Framework;

namespace Inputting.Tests
{
    public class InputSchedulerTest
    {
        /// <summary>
        /// Tests that with smoothing disabled, the durations should just stay constant
        /// if enqueuing and dequeuing happens at an equal pace.
        /// </summary>
        [Test]
        public void TestConstantSpeed()
        {
            // given
            const float targetDurationSeconds = 5f;
            var inputScheduler = new InputBufferQueue<int>(new InputBufferQueue<int>.Config(
                BufferLengthSeconds: targetDurationSeconds,
                SpeedupRate: 1f,
                SlowdownRate: 1f,
                MinInputDuration: 0f,
                MaxInputDuration: 999f));

            // when: for every input dequeued another one gets queued
            inputScheduler.Enqueue(1);
            (int v1, float t1) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(2);
            (int v2, float t2) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(3);
            (int v3, float t3) = inputScheduler.Dequeue();

            // then: all durations should be equal
            Assert.That((1, 2, 3), Is.EqualTo((v1, v2, v3)));
            Assert.That(t1, Is.EqualTo(targetDurationSeconds));
            Assert.That(t2, Is.EqualTo(targetDurationSeconds));
            Assert.That(t3, Is.EqualTo(targetDurationSeconds));
        }

        /// <summary>
        /// Tests that the buffer works as expected.
        /// If the buffer duration is n, and there are m inputs per second,
        /// once the buffer reached n*m items the duration should simply be 1/m.
        /// </summary>
        [Test]
        public void TestBufferLength()
        {
            // given
            const float targetDurationSeconds = 3f;
            const float inputsPerSecond = 5f;
            var inputScheduler = new InputBufferQueue<int>(new InputBufferQueue<int>.Config(
                BufferLengthSeconds: targetDurationSeconds,
                SpeedupRate: 1f,
                SlowdownRate: 1f,
                MinInputDuration: 0f,
                MaxInputDuration: 999f));
            for (int i = 0; i < targetDurationSeconds * inputsPerSecond; i++)
            {
                inputScheduler.Enqueue(i);
            }

            // when
            (int _, float t1) = inputScheduler.Dequeue();

            // then: duration should be 1 over inputs per second
            Assert.That(t1, Is.EqualTo(1f / inputsPerSecond).Within(0.00001f));
        }

        /// <summary>
        /// Tests that input durations get smaller if more inputs get queued than dequeued.
        /// </summary>
        [Test]
        public void TestAccelerate()
        {
            // given
            const float targetDurationSeconds = 5f;
            var inputScheduler = new InputBufferQueue<int>(new InputBufferQueue<int>.Config(
                BufferLengthSeconds: targetDurationSeconds,
                SpeedupRate: 1f,
                SlowdownRate: 1f,
                MinInputDuration: 0f,
                MaxInputDuration: 999f));

            // when: more items are being queued than dequeued
            inputScheduler.Enqueue(1);
            inputScheduler.Enqueue(2);
            (int v1, float t1) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(3);
            inputScheduler.Enqueue(4);
            (int v2, float t2) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(5);
            inputScheduler.Enqueue(6);
            (int v3, float t3) = inputScheduler.Dequeue();

            // then: durations should get smaller
            Assert.That((1, 2, 3), Is.EqualTo((v1, v2, v3)));
            Assert.That(t1, Is.EqualTo(targetDurationSeconds / 2f));
            Assert.That(t2, Is.EqualTo(targetDurationSeconds / 3f));
            Assert.That(t3, Is.EqualTo(targetDurationSeconds / 4f));
        }

        /// <summary>
        /// Tests that input durations get larger if fewer inputs get queued than dequeued.
        /// </summary>
        [Test]
        public void TestDecelerate()
        {
            // given
            const float targetDurationSeconds = 5f;
            var inputScheduler = new InputBufferQueue<int>(new InputBufferQueue<int>.Config(
                BufferLengthSeconds: targetDurationSeconds,
                SpeedupRate: 1f,
                SlowdownRate: 1f,
                MinInputDuration: 0f,
                MaxInputDuration: 999f));

            // when: fewer items are being queued than dequeued
            inputScheduler.Enqueue(1);
            inputScheduler.Enqueue(2);
            inputScheduler.Enqueue(3);
            inputScheduler.Enqueue(4);
            (int v1, float t1) = inputScheduler.Dequeue();
            (int v2, float t2) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(5);
            (int v3, float t3) = inputScheduler.Dequeue();
            (int v4, float t4) = inputScheduler.Dequeue();

            // then: durations should get longer
            Assert.That((1, 2, 3, 4), Is.EqualTo((v1, v2, v3, v4)));
            Assert.That(t1, Is.EqualTo(targetDurationSeconds / 4f));
            Assert.That(t2, Is.EqualTo(targetDurationSeconds / 3f));
            Assert.That(t3, Is.EqualTo(targetDurationSeconds / 3f));
            Assert.That(t4, Is.EqualTo(targetDurationSeconds / 2f));
        }

        /// <summary>
        /// Tests that for smoothing enabled (speedup rate),
        /// input durations are only slowly getting adjusted.
        /// </summary>
        [Test]
        public void TestSmooth()
        {
            // given
            const float targetDurationSeconds = 5f;
            var inputScheduler = new InputBufferQueue<int>(new InputBufferQueue<int>.Config(
                BufferLengthSeconds: targetDurationSeconds,
                SpeedupRate: 0.5f,
                SlowdownRate: 1.0f,
                MinInputDuration: 0f,
                MaxInputDuration: 999f));

            // when: items are being queued and dequeued
            inputScheduler.Enqueue(1);
            (int v1, float t1) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(2);
            (int v2, float t2) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(3);
            inputScheduler.Enqueue(4);
            (int v3, float t3) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(5);
            (int v4, float t4) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(6);
            (int v5, float t5) = inputScheduler.Dequeue();
            inputScheduler.Enqueue(7);
            (int v6, float t6) = inputScheduler.Dequeue();

            // then: durations should get smaller, but smoothly
            Assert.That((1, 2, 3, 4, 5, 6), Is.EqualTo((v1, v2, v3, v4, v5, v6)));
            Assert.That(t1, Is.EqualTo(targetDurationSeconds));
            Assert.That(t2, Is.EqualTo(targetDurationSeconds));
            Assert.That(t3, Is.EqualTo(targetDurationSeconds * (0.5f + 0.5f / 2)));
            Assert.That(t4, Is.EqualTo(targetDurationSeconds * (0.5f + 0.5f / 4)));
            Assert.That(t5, Is.EqualTo(targetDurationSeconds * (0.5f + 0.5f / 8)));
            Assert.That(t6, Is.EqualTo(targetDurationSeconds * (0.5f + 0.5f / 16)));
        }

        [Test]
        public void TestMaxCapacity()
        {
            var inputScheduler = new InputBufferQueue<int>(new InputBufferQueue<int>.Config(MaxBufferLength: 2));
            Assert.That(inputScheduler.Enqueue(1), Is.True);
            Assert.That(inputScheduler.Enqueue(2), Is.True);
            Assert.That(inputScheduler.Enqueue(3), Is.False);

            Assert.That(inputScheduler.Dequeue().Item1, Is.EqualTo(1));
            Assert.That(inputScheduler.Dequeue().Item1, Is.EqualTo(2));
            Assert.That(inputScheduler.IsEmpty, Is.True);
        }
    }
}
