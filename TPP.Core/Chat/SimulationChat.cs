using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TPP.Core.Configuration;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Chat;

/// Simulates incoming chat activity
public sealed class SimulationChat(
    string name,
    ILoggerFactory loggerFactory,
    ConnectionConfig.Simulation config,
    IUserRepo userRepo)
    : IChat
{
    private readonly ILogger<SimulationChat> _logger = loggerFactory.CreateLogger<SimulationChat>();

    public string Name { get; } = name;
    public event EventHandler<MessageEventArgs>? IncomingMessage;

    public async Task Start(CancellationToken cancellationToken)
    {
        Random random = new();
        var timeSinceStart = Stopwatch.StartNew();
        long numInputsSentSinceStart = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            double targetNumInputs = config.InputsPerSecond * timeSinceStart.Elapsed.TotalSeconds;
            while (numInputsSentSinceStart < targetNumInputs)
            {
                string name = Path.GetRandomFileName();
                User user = await userRepo.RecordUser(
                    new UserInfo("simulation-" + name.ToLower(), name, name.ToLower()));
                Message message = new(
                    user,
                    "a",
                    new MessageSource.PrimaryChat(),
                    string.Empty
                );
                IncomingMessage?.Invoke(this, new MessageEventArgs(message));
                numInputsSentSinceStart++;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(10, 100)), cancellationToken);
        }
    }

    private static Task PrintAction(string message)
    {
        Console.Out.WriteLine($"=== SIMULATED CHAT: {message} ===");
        return Task.CompletedTask;
    }

    public Task SendMessage(string message, Message? responseTo = null) => PrintAction($"chat: {message}");
    public Task SendWhisper(User target, string message) => PrintAction($"whisper to {target}: {message}");
}
