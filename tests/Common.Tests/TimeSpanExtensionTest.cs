using System;
using NUnit.Framework;

namespace Common.Tests;

public class TimeSpanExtensionTest
{
    [Test]
    public void test_to_human_readable()
    {
        Assert.That(TimeSpan.Zero.ToHumanReadable(), Is.EqualTo("0s"));
        Assert.That(TimeSpan.Parse("0:00:00.001").ToHumanReadable(), Is.EqualTo("1ms"));
        Assert.That(TimeSpan.Parse("0:00:01.000").ToHumanReadable(), Is.EqualTo("1s"));
        Assert.That(TimeSpan.Parse("0:01:00.000").ToHumanReadable(), Is.EqualTo("1m"));
        Assert.That(TimeSpan.Parse("1:00:00.000").ToHumanReadable(), Is.EqualTo("1h"));
        Assert.That(TimeSpan.Parse("1.00:00:00.000").ToHumanReadable(), Is.EqualTo("1d"));
        Assert.That(TimeSpan.Parse("1.01:01:01.001").ToHumanReadable(), Is.EqualTo("1d1h1m1s1ms"));
        Assert.That(TimeSpan.Parse("11.11:11:11.111").ToHumanReadable(), Is.EqualTo("11d11h11m11s111ms"));
    }
}
