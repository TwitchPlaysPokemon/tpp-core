using System.Collections.Generic;
using System.Threading.Tasks;
using TPP.Core.Chat;

namespace TPP.Core.Commands.Definitions;

public class JoinChatCommands : ICommandCollection
{
    public IEnumerable<Command> Commands => new[]
    {
        new Command("join", JoinChannel) { Description = "Joins channel TODO" },
        new Command("leave", LeaveChannel) { Description = "Leaved channel TODO" },
    };

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
}
