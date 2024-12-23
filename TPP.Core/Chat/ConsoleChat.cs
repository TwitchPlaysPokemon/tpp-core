using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Chat
{
    /// Simulates a chat using stdin and stdout
    public sealed class ConsoleChat : IChat
    {
        private readonly ILogger<ConsoleChat> _logger;
        private readonly ConnectionConfig.Console _config;
        private readonly IUserRepo _userRepo;

        public ConsoleChat(
            string name,
            ILoggerFactory loggerFactory,
            ConnectionConfig.Console config,
            IUserRepo userRepo)
        {
            Name = name;
            _logger = loggerFactory.CreateLogger<ConsoleChat>();
            _config = config;
            _userRepo = userRepo;
        }

        public Task SendMessage(string? message, Message? responseTo = null)
        {
            if (responseTo != null) message = $"@{responseTo.User.Name} " + message;
            Console.WriteLine(message);
            return Task.CompletedTask;
        }

        public Task SendWhisper(User target, string message)
        {
            Console.WriteLine($">{target.Name}: {message}");
            return Task.CompletedTask;
        }

        public string Name { get; }
        public event EventHandler<MessageEventArgs>? IncomingMessage;

        private async Task ReadInput(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Put Console.In.ReadLineAsync onto the thread pool, because Console.In's TextReader#ReadLineAsync
                // surprisingly actually runs synchronous, and we do not want to block the async runtime.
                string? maybeLine = await Task.Run(async () => await Console.In.ReadLineAsync(cancellationToken));
                if (maybeLine == null) break;
                string line = maybeLine;
                string username = _config.Username;
                if (line.StartsWith('#'))
                {
                    string[] split = line.Split(' ', count: 2);
                    username = split[0][1..];
                    line = split.ElementAtOrDefault(1) ?? string.Empty;
                }
                string simpleName = username.ToLower();

                MessageSource source = new MessageSource.PrimaryChat();
                if (line.StartsWith('>'))
                {
                    line = line[1..];
                    source = new MessageSource.Whisper();
                }

                // re-use already existing users if they exist, otherwise it gets confusing when you want to impersonate
                // an existing user and it creates a new user with the same name instead, breaking the expectation that
                // usernames are unique.
                string userId = "console-" + simpleName;
                User user = await _userRepo.FindBySimpleName(simpleName)
                            ?? await _userRepo.RecordUser(new UserInfo(userId, username, simpleName));
                long nowTs = SystemClock.Instance.GetCurrentInstant().ToUnixTimeSeconds();
                // Providing a fake irc line makes dualcore mode work.
                // Otherwise old core couldn't parse any of these messages.
                string rawIrcMessage =
                    $"@badge-info=;badges=;color=#FFFFFF;display-name={username};emotes=;id={new Guid()};" +
                    $"room-id=console;tmi-sent-ts={nowTs};user-id={userId};user-type= " +
                    $":{simpleName}!{simpleName}@{simpleName}.tmi.twitch.tv " +
                    $"PRIVMSG #console :{line}";
                IncomingMessage?.Invoke(this, new MessageEventArgs(new Message(user, line, source, rawIrcMessage)));
            }
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            Console.Out.WriteLine($"Chatting via console is now enabled. You are known as '{_config.Username}'.");
            Console.Out.WriteLine(
                "Prefixing a message with '#username ' will post as a different user, e.g. '#someone !help'");
            Console.Out.WriteLine("Prefixing a message with '>' will make it a whisper, e.g. '>balance'");
            Console.Out.WriteLine("You can combine both, e.g. '#someone >balance'");
            await ReadInput(cancellationToken);
        }
    }
}
