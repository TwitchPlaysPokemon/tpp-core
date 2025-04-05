using System;
using System.Threading.Tasks;
using TPP.Core.Moderation;
using TPP.Model;

namespace TPP.Core.Chat;

public class MessageEventArgs(Message message) : EventArgs
{
    public Message Message { get; } = message;
}

public interface IMessageSender
{
    Task SendMessage(string message, Message? responseTo = null);
    Task SendWhisper(User target, string message);
}

public interface IChatModeChanger
{
    public Task EnableEmoteOnly();
    public Task DisableEmoteOnly();
}

public interface IMessageSource
{
    event EventHandler<MessageEventArgs> IncomingMessage;
}

/// <summary>
/// Interface that describes a chat where messages can be received from and sent to,
/// in the context of a connection lifecycle.
/// Classes that implement this interface may also implement some of these to enable some additional features:
/// <see cref="IChatModeChanger"/> for mod commands to change the chat mode,
/// <see cref="IExecutor"/> for automated chat moderation
/// </summary>
public interface IChat : IMessageSender, IMessageSource, IWithLifecycle
{
    string Name { get; }
}
