using NUnit.Framework;

namespace TPP.Inputting.Tests;

public class InputSchedulerTest
{
    /// <summary>
    /// Tests that with smoothing disabled, the durations should just stay constant
    /// if enqueuing and and dequeuing happens at an equal pace.
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
        Assert.AreEqual((1, 2, 3), (v1, v2, v3));
        Assert.AreEqual(targetDurationSeconds, t1);
        Assert.AreEqual(targetDurationSeconds, t2);
        Assert.AreEqual(targetDurationSeconds, t3);
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
        Assert.AreEqual(1f / inputsPerSecond, t1, 0.00001f);
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
        Assert.AreEqual((1, 2, 3), (v1, v2, v3));
        Assert.AreEqual(targetDurationSeconds / 2f, t1);
        Assert.AreEqual(targetDurationSeconds / 3f, t2);
        Assert.AreEqual(targetDurationSeconds / 4f, t3);
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
        Assert.AreEqual((1, 2, 3, 4), (v1, v2, v3, v4));
        Assert.AreEqual(targetDurationSeconds / 4f, t1);
        Assert.AreEqual(targetDurationSeconds / 3f, t2);
        Assert.AreEqual(targetDurationSeconds / 3f, t3);
        Assert.AreEqual(targetDurationSeconds / 2f, t4);
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
        Assert.AreEqual((1, 2, 3, 4, 5, 6), (v1, v2, v3, v4, v5, v6));
        Assert.AreEqual(targetDurationSeconds, t1);
        Assert.AreEqual(targetDurationSeconds, t2);
        Assert.AreEqual(targetDurationSeconds * (0.5f + 0.5f / 2), t3);
        Assert.AreEqual(targetDurationSeconds * (0.5f + 0.5f / 4), t4);
        Assert.AreEqual(targetDurationSeconds * (0.5f + 0.5f / 8), t5);
        Assert.AreEqual(targetDurationSeconds * (0.5f + 0.5f / 16), t6);
    }

    [Test]
    public void TestMaxCapacity()
    {
        var inputScheduler = new InputBufferQueue<int>(new InputBufferQueue<int>.Config(MaxBufferLength: 2));
        Assert.IsTrue(inputScheduler.Enqueue(1));
        Assert.IsTrue(inputScheduler.Enqueue(2));
        Assert.IsFalse(inputScheduler.Enqueue(3));

        Assert.AreEqual(1, inputScheduler.Dequeue().Item1);
        Assert.AreEqual(2, inputScheduler.Dequeue().Item1);
        Assert.IsTrue(inputScheduler.IsEmpty);
    }
}
