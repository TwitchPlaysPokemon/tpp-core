using NUnit.Framework;
using Core.Chat;

namespace Core.Tests.Chat
{
    public class KeyCountPrioritizedQueueTest
    {
        [Test]
        public void keeps_per_user_order()
        {
            KeyCountPrioritizedQueue<string, string> queue = new();

            queue.Enqueue("user2", "A");
            queue.Enqueue("user2", "B");
            queue.Enqueue("user1", "C");
            queue.Enqueue("user1", "D");
            queue.Enqueue("user1", "E");
            queue.Enqueue("user3", "F");

            Assert.That(queue.Dequeue()!.Value, Is.EqualTo(("user3", "F")));
            Assert.That(queue.Dequeue()!.Value, Is.EqualTo(("user2", "A")));
            Assert.That(queue.Dequeue()!.Value, Is.EqualTo(("user2", "B")));
            Assert.That(queue.Dequeue()!.Value, Is.EqualTo(("user1", "C")));
            Assert.That(queue.Dequeue()!.Value, Is.EqualTo(("user1", "D")));
            Assert.That(queue.Dequeue()!.Value, Is.EqualTo(("user1", "E")));
            Assert.That(queue.Dequeue(), Is.Null);
        }
    }
}
