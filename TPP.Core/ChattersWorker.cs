using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Chat;
using TPP.Core.Configuration;
using TPP.Core.Utils;
using TPP.Persistence;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;

namespace TPP.Core;

public sealed class ChattersWorker(
    ILoggerFactory loggerFactory,
    IClock clock,
    TwitchApiProvider twitchApiProvider,
    IChattersSnapshotsRepo chattersSnapshotsRepo,
    ConnectionConfig.Twitch chatConfig
) : IWithLifecycle
{
    private readonly ILogger<ChattersWorker> _logger = loggerFactory.CreateLogger<ChattersWorker>();
    private readonly TimeSpan _delay = chatConfig.GetChattersInterval!.Value.ToTimeSpan();

    public async Task Start(CancellationToken cancellationToken)
    {
        try { await Task.Delay(_delay, cancellationToken); }
        catch (OperationCanceledException) { return; }
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                List<Chatter> chatters;
                try { chatters = await GetChatters(cancellationToken); }
                catch(OperationCanceledException) { break; }

                ImmutableList<string> chatterNames = chatters.Select(c => c.UserLogin).ToImmutableList();
                ImmutableList<string> chatterIds = chatters.Select(c => c.UserId).ToImmutableList();
                await chattersSnapshotsRepo.LogChattersSnapshot(
                    chatterNames, chatterIds, chatConfig.Channel, clock.GetCurrentInstant());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed retrieving chatters list");
            }

            try { await Task.Delay(_delay, cancellationToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task<List<Chatter>> GetChatters(CancellationToken cancellationToken)
    {
        List<Chatter> chatters = [];
        string? nextCursor = null;
        do
        {
            Task<GetChattersResponse> getChattersTask = (await twitchApiProvider.Get()).Helix.Chat
                .GetChattersAsync(chatConfig.ChannelId, chatConfig.UserId, first: 1000, after: nextCursor);
            if (await Task.WhenAny(getChattersTask, cancellationToken.WhenCanceled()) != getChattersTask)
                // canceled, but GetChattersAsync doesn't take a cancellation token directly
                throw new TaskCanceledException();
            GetChattersResponse getChattersResponse = await getChattersTask;
            chatters.AddRange(getChattersResponse.Data);
            nextCursor = getChattersResponse.Pagination?.Cursor;
        } while (nextCursor != null);
        _logger.LogDebug("Retrieved {NumChatters} chatters", chatters.Count);
        return chatters;
    }
}
