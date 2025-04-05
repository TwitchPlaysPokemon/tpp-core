using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Core.Chat;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class JoinChatCommands(ICoStreamChannelsRepo coStreamChannelsRepo) : ICommandCollection
{
    public IEnumerable<Command> Commands =>
    [
        new Command("join", JoinChannel)
        {
            Description = "Makes the TPP bot join your channel and consume inputs from there, " +
                          "given the current run supports it. This is for co-streaming TPP. " +
                          "Note that you must be live, and the bot will auto-leave again once you go offline."
        },
        new Command("leave", LeaveChannel)
        {
            Description = "If you previously issues a !join command, makes the TPP bot leave your channel again."
        },
        new Command("costreams", ListCostreams)
        {
            Aliases = ["costreamers"],
            Description = "Lists all channels that are currently co-streaming TPP."
        }
    ];

    public async Task<CommandResult> JoinChannel(CommandContext context)
    {
        if (context.Source is not TwitchChat twitchChat)
            return new CommandResult { Response = "Having the bot join your channel is not supported from this chat" };
        TwitchEventSubChat.JoinResult result = await twitchChat.TwitchEventSubChat.Join(context.Message.User.Id);
        string response = result switch
        {
            TwitchEventSubChat.JoinResult.Ok => "Successfully joined channel",
            TwitchEventSubChat.JoinResult.NotEnabled => "Channel joining is currently not enabled",
            TwitchEventSubChat.JoinResult.AlreadyJoined => "Already joined",
            TwitchEventSubChat.JoinResult.UserNotFound => "You don't exist according to Twitch",
            TwitchEventSubChat.JoinResult.StreamOffline => "You are not live",
        };
        return new CommandResult { Response = response };
    }

    public async Task<CommandResult> LeaveChannel(CommandContext context)
    {
        if (context.Source is not TwitchChat twitchChat)
            return new CommandResult { Response = "Having the bot leave your channel is not supported from this chat" };
        TwitchEventSubChat.LeaveResult result = await twitchChat.TwitchEventSubChat.Leave(context.Message.User.Id);
        string response = result switch
        {
            TwitchEventSubChat.LeaveResult.Ok => "Successfully left channel",
            TwitchEventSubChat.LeaveResult.NotJoined => "Not joined",
        };
        return new CommandResult { Response = response };
    }

    public async Task<CommandResult> ListCostreams(CommandContext context)
    {
        IImmutableSet<string> joinedChannels = await coStreamChannelsRepo.GetJoinedChannels();
        return new CommandResult { Response = joinedChannels.Count == 0
            ? "There are currently no channels co-streaming TPP"
            : "The following channels are currently co-streaming TPP: " + string.Join(", ", joinedChannels) };
    }
}
