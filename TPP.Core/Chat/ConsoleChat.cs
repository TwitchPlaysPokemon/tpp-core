using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Persistence.Models;
using TPP.Persistence.Repos;

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

        public Task SendMessage(string message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }

        public Task SendWhisper(User target, string message)
        {
            Console.WriteLine($">{target.Name}: {message}");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Console.In.Close();
        }

        public string Name { get; }
        public event EventHandler<MessageEventArgs>? IncomingMessage;

        private async Task ReadInput()
        {
            string? line;
            while ((line = await Console.In.ReadLineAsync()) != null)
            {
                string username = _config.Username;
                if (line.StartsWith('#'))
                {
                    string[] split = line.Split(' ', count: 2);
                    username = split[0][1..];
                    line = split[1];
                }
                string simpleName = username.ToLower();

                var source = MessageSource.Chat;
                if (line.StartsWith('>'))
                {
                    line = line[1..];
                    source = MessageSource.Whisper;
                }

                // re-use already existing users if they exist, otherwise it gets confusing when you want to impersonate
                // an existing user and it creates a new user with the same name instead, breaking the expectation that
                // usernames are unique.
                User user = await _userRepo.FindBySimpleName(simpleName)
                            ?? await _userRepo.RecordUser(new UserInfo("console-" + simpleName, username, simpleName));
                IncomingMessage?.Invoke(this, new MessageEventArgs(new Message(user, line, source, string.Empty)));
            }
        }

        public void Connect()
        {
            Task.Run(ReadInput).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    _logger.LogError("console read task failed", task.Exception);
                else
                    _logger.LogInformation("console read task finished");
            });

            Console.Out.WriteLine($"Chatting via console is now enabled. You are known as '{_config.Username}'.");
            Console.Out.WriteLine(
                "Prefixing a message with '#username ' will post as a different user, e.g. '#someone !help'");
            Console.Out.WriteLine("Prefixing a message with '>' will make it a whisper, e.g. '>balance'");
            Console.Out.WriteLine("You can combine both, e.g. '#someone >balance'");
        }

        private static Task PrintAction(string message)
        {
            Console.Out.WriteLine($"=== {message} ===");
            return Task.CompletedTask;
        }

        public Task EnableEmoteOnly() => PrintAction("enable emote only");
        public Task DisableEmoteOnly() => PrintAction("disable emote only");
        public Task DeleteMessage(string messageId) => PrintAction($"delete message with id {messageId}");
        public Task Timeout(User user, string? message, Duration duration) =>
            PrintAction($"time out {user.Name} for {duration}");
    }
}
