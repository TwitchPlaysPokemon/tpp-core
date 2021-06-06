using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Chat
{
    /// Simulates incoming chat activity
    public sealed class SimulationChat : IChat
    {
        private readonly ILogger<SimulationChat> _logger;
        private readonly ConnectionConfig.Simulation _config;
        private readonly IUserRepo _userRepo;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public SimulationChat(
            string name,
            ILoggerFactory loggerFactory,
            ConnectionConfig.Simulation config,
            IUserRepo userRepo)
        {
            Name = name;
            _logger = loggerFactory.CreateLogger<SimulationChat>();
            _config = config;
            _userRepo = userRepo;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        public string Name { get; }
        public event EventHandler<MessageEventArgs>? IncomingMessage;

        public async Task Simulate()
        {
            Random random = new();
            var timeSinceStart = Stopwatch.StartNew();
            long numInputsSentSinceStart = 0;
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                double targetNumInputs = _config.InputsPerSecond * timeSinceStart.Elapsed.TotalSeconds;
                while (numInputsSentSinceStart < targetNumInputs)
                {
                    string name = Path.GetRandomFileName();
                    User user = await _userRepo.RecordUser(
                        new UserInfo("simulation-" + name.ToLower(), name, name.ToLower()));
                    Message message = new(
                        user,
                        "a",
                        MessageSource.Chat,
                        string.Empty
                    );
                    IncomingMessage?.Invoke(this, new MessageEventArgs(message));
                    numInputsSentSinceStart++;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(random.Next(10, 100)), _cancellationTokenSource.Token);
            }
        }

        public void Connect()
        {
            Task.Run(Simulate).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    _logger.LogError(task.Exception, "simulation task failed");
                else
                    _logger.LogInformation("simulation task finished");
            });
        }

        public Task SendMessage(string message) => throw new NotImplementedException();
        public Task SendWhisper(User target, string message) => throw new NotImplementedException();
        public Task EnableEmoteOnly() => throw new NotImplementedException();
        public Task DisableEmoteOnly() => throw new NotImplementedException();
        public Task DeleteMessage(string messageId) => throw new NotImplementedException();
        public Task Timeout(User user, string? message, Duration duration) => throw new NotImplementedException();
    }
}
