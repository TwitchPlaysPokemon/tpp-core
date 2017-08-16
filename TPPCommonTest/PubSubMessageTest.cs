using System;
using TPPCommon.PubSub;
using TPPCommon.PubSub.Messages;
using Xunit;

namespace TPPCommonTest
{
    /// <summary>
    /// Test definition of PubSub message classes and their respective topic.
    /// </summary>
    public class PubSubMessageTest
    {
        private const Topic TestTopic = Topic.CurrentSongInfo; // can be any topic 
        
        [Topic(TestTopic)]
        class TestMessageWithTopicAttribute : PubSubMessage
        {
        }

        class TestMessageWithoutTopicAttribute : PubSubMessage
        {
        }
        
        [Topic(TestTopic)]
        class TestMessageNotSubclass
        {
        }

        [Fact]
        public void TestTopicNameWithAttribute()
        {
            var message = new TestMessageWithTopicAttribute();
            Assert.Equal(TestTopic, message.GetTopic());
        }
        
        [Fact]
        public void TestTopicNameWithoutAttribute()
        {
            // retrieving the topic for a message without the topic-attribute should raise an error.
            var message = new TestMessageWithoutTopicAttribute();
            Assert.Throws<ArgumentException>(() => message.GetTopic());
        }

        [Fact]
        public void TestTopicForNonSubclass()
        {
            // retrieving the topic for a class that doesn't inherit PubSubMessage should raise an error.
            Assert.Throws<ArgumentException>(() => PubSubMessage.GetTopicForMessageType(typeof(TestMessageNotSubclass)));
        }
    }
}