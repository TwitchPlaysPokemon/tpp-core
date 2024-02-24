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
        TwitchChat.JoinResult result = await twitchChat.Join(context.Message.User.Name);
        string response = result switch
        {
            TwitchChat.JoinResult.Ok => "Successfully joined channel",
            TwitchChat.JoinResult.NotEnabled => "Channel joining is currently not enabled",
            TwitchChat.JoinResult.AlreadyJoined => "Already joined",
            TwitchChat.JoinResult.UserNotFound => "You don't exist according to Twitch",
            TwitchChat.JoinResult.StreamOffline => "You are not live",
        };
        return new CommandResult { Response = response };
    }

    public async Task<CommandResult> LeaveChannel(CommandContext context)
    {
        if (context.Source is not TwitchChat twitchChat)
            return new CommandResult { Response = "Having the bot leave your channel is not supported from this chat" };
        TwitchChat.LeaveResult result = await twitchChat.Leave(context.Message.User.Name);
        string response = result switch
        {
            TwitchChat.LeaveResult.Ok => "Successfully left channel",
            TwitchChat.LeaveResult.NotJoined => "Not joined",
        };
        return new CommandResult { Response = response };
    }
}
