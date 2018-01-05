using System;
using TPPCore.Service.Example.Parrot;
using Xunit;

namespace TPPCore.Service.Example.Parrot.Tests
{
    public class ModelTest
    {
        [Fact]
        public void TestModelRepeat()
        {
            var model = new Model();

            Assert.Equal(0, model.RepeatCount);
            Assert.Equal("hello world!", model.CurrentMessage);
            Assert.Equal(0, model.RecentMessages.Count);

            model.Repeat();

            Assert.Equal(1, model.RepeatCount);
            Assert.Equal("hello world!", model.CurrentMessage);
            Assert.Equal(1, model.RecentMessages.Count);
        }

        [Fact]
        public void TestModelNewMessage()
        {
            var model = new Model();

            model.Repeat();

            model.RepeatNewMessage("hi");

            Assert.Equal(0, model.RepeatCount);
            Assert.Equal("hi", model.CurrentMessage);
            Assert.Equal(2, model.RecentMessages.Count);
        }
    }
}
