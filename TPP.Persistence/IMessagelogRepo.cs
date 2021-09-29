namespace TPP.Persistence;

public interface IMessagelogRepo
{
    Task<Messagelog> LogChat(string userId, string ircLine, string message, Instant timestamp);
}
