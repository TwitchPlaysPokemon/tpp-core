using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using TPP.Core.Configuration;
using TPP.Core.Utils;
using TPP.Persistence;
using TwitchLib.Api.Helix.Models.Chat.GetChatters;

namespace TPP.Core;

public sealed class ChattersWorker(
    ILoggerFactory loggerFactory,
    IClock clock,
    TwitchApi twitchApi,
    IChattersSnapshotsRepo chattersSnapshotsRepo,
    ConnectionConfig.Twitch chatConfig,
    IUserRepo userRepo
) : IWithLifecycle
{
    private readonly ILogger<ChattersWorker> _logger = loggerFactory.CreateLogger<ChattersWorker>();
    private readonly TimeSpan _delay = chatConfig.GetChattersInterval!.Value.ToTimeSpan();

    public async Task Start(CancellationToken cancellationToken)
    {
        do
        {
            await Task.Delay(_delay, cancellationToken);
            try
            {
                List<Chatter> chatters = await GetChatters(cancellationToken);

                ImmutableList<string> chatterNames = chatters.Select(c => c.UserLogin).ToImmutableList();
                ImmutableList<string> chatterIds = chatters.Select(c => c.UserId).ToImmutableList();

                // Record all yet unknown users. Makes other code that retrieves users via chatters easier,
                // because that code can then rely on all users from the chatters snapshot actually existing in the DB.
                HashSet<string> knownIds = (await userRepo.FindByIds(chatterIds)).Select(u => u.Id).ToHashSet();
                HashSet<string> unknownIds = chatterIds.Except(knownIds).ToHashSet();
                foreach (Chatter newUser in chatters.Where(ch => unknownIds.Contains(ch.UserId)))
                    await userRepo.RecordUser(new UserInfo(newUser.UserId, newUser.UserName, newUser.UserLogin));

                await chattersSnapshotsRepo.LogChattersSnapshot(
                    chatterNames, chatterIds, chatConfig.Channel, clock.GetCurrentInstant());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed retrieving chatters list");
            }
        } while (!cancellationToken.IsCancellationRequested);
    }

    private async Task<List<Chatter>> GetChatters(CancellationToken cancellationToken)
    {
        List<Chatter> chatters = [];
        string? nextCursor = null;
        do
        {
            Task<GetChattersResponse> getChattersTask = twitchApi
                .GetChattersAsync(chatConfig.ChannelId, chatConfig.UserId, first: 1000, after: nextCursor);
            if (await Task.WhenAny(getChattersTask, cancellationToken.WhenCanceled()) != getChattersTask)
                // canceled, but GetChattersAsync doesn't take a cancellation token directly
                throw new TaskCanceledException();
            GetChattersResponse getChattersResponse = await getChattersTask;
            chatters.AddRange(getChattersResponse.Data);
            nextCursor = getChattersResponse.Pagination?.Cursor;
        } while (nextCursor != null);
        _logger.LogDebug("Retrieved {NumChatters} chatters: {ChatterNames}",
            chatters.Count, string.Join(", ", chatters.Select(c => c.UserLogin)));
        return chatters;
    }
}
