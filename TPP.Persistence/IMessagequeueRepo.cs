namespace TPP.Persistence;

public interface IMessagequeueRepo
{
    Task<MessagequeueItem> EnqueueMessage(string ircLine);
}
