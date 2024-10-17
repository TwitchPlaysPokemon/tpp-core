using System.Threading.Tasks;

namespace TPP.Core.Chat;

public interface ICommandHandler
{
    public Task ProcessIncomingMessage(IChat chat, Message message);
}
