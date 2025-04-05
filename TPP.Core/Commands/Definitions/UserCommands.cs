using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using TPP.Model;
using TPP.Persistence;

namespace TPP.Core.Commands.Definitions;

public class UserCommands(IUserRepo userRepo) : ICommandCollection
{
    public IEnumerable<Command> Commands =>
    [
        new("displayname", SetDisplayName)
        {
            Description = "Set the capitalization of your display name as it appears on stream. " +
                          "Only needed by users with special characters in their display name. " +
                          "Argument: <new name>"
        },
        new("list", List)
        {
            Description = "List all of the people who have a given role. " +
                          "Arguments: <role>"
        },
        new("showroles", ShowRoles)
        {
            Aliases = ["roles"],
            Description = "Show which roles a user has. " +
                          "Arguments: <user>"
        },
        new Command("operators", Ops) { Aliases = ["ops"], Description = "Alias for '!list operator'" }.WithGlobalCooldown(Duration.FromSeconds(30)),
        new Command("moderators", Mods) { Aliases = ["mods"], Description = "Alias for '!list moderator'" }.WithGlobalCooldown(Duration.FromSeconds(30))
    ];

    public async Task<CommandResult> SetDisplayName(CommandContext context)
    {
        User user = context.Message.User;
        if (user.TwitchDisplayName.ToLowerInvariant() == user.SimpleName)
        {
            return new CommandResult
            {
                Response = "you don't have any special characters in your name " +
                           "and can therefore still change it in your twitch settings"
            };
        }
        string newName = await context.ParseArgs<string>();
        if (newName.ToLower() != user.SimpleName)
        {
            return new CommandResult
            {
                Response = "your new display name may only differ from your login name in capitalization"
            };
        }
        await userRepo.SetDisplayName(user, newName);
        return new CommandResult { Response = $"your display name has been updated to '{newName}'" };
    }

    public async Task<CommandResult> List(CommandContext context)
    {
        Role role = await context.ParseArgs<Role>();
        List<User> users = await userRepo.FindAllByRole(role);

        return new CommandResult
        {
            Response = users.Count > 0
                ? $"The users with the '{role.ToString()}' role are: {string.Join(", ", users.Select(u => u.Name))}"
                : $"There are no users with the '{role.ToString()}' role."
        };
    }

    public async Task<CommandResult> ShowRoles(CommandContext context)
    {
        User user = await context.ParseArgs<User>();

        return new CommandResult
        {
            Response = user.Roles.Count > 0
                ? $"{user.Name} has the roles: {string.Join(", ", user.Roles)}"
                : $"{user.Name} has no roles"
        };
    }

    private Task<CommandResult> Ops(CommandContext context) =>
        List(context with { Args = ImmutableList.Create("operator") });

    private Task<CommandResult> Mods(CommandContext context) =>
        List(context with { Args = ImmutableList.Create("moderator") });
}
