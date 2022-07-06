using System.Collections.Generic;
using System.Threading.Tasks;

namespace TPP.Core.Commands.Definitions
{
    public class HelpCommand
    {
        public Command Command => new("help", ctx => Task.FromResult(Execute(ctx)))
        {
            Aliases = new[] { "info" },
            Description = "Get general help, or info on a specific command like: \"!help balance\""
        };

        private readonly CommandProcessor _commandProcessor;
        public HelpCommand(CommandProcessor commandProcessor) => _commandProcessor = commandProcessor;

        private CommandResult Execute(CommandContext context)
        {
            if (context.Args.Count == 0)
                return new CommandResult
                {
                    Response =
                        "Read the description for more info! Or get info on a specific command like: " +
                        "\"!help balance\". Command reference: https://twitchplayspokemon.tv/commands"
                };
            if (context.Args.Count > 1)
                return new CommandResult { Response = "Commands do not contain spaces. Ex: \"!help selectbadge\"" };

            string commandName = context.Args[0];
            Command? command = _commandProcessor.FindCommand(commandName);
            if (command != null)
                return new CommandResult { Response = command.Value.Description };

            // dual core compatibility: supply help messages for commands that still live in the old core
            if (_oldCoreCommandDescriptions.TryGetValue(commandName.ToLower(), out string? desc))
                return new CommandResult { Response = desc };

            return new CommandResult { Response = $"Command not found: {commandName}" };
        }

        private readonly Dictionary<string, string> _oldCoreCommandDescriptions = new()
        {
            // @formatter:off

            // operator commands
            ["stormadd"] = "Operators only: Add an amount to tokens to the current/next storm. Argument: t<number of tokens>",
            ["addstorm"] = "Operators only: Add an amount to tokens to the current/next storm. Argument: t<number of tokens>",
            ["start"] = "Operators only: Start the main loop, if it is not running.",
            ["showseasonsleaderboard"] = "Operators only: Show the season's leaderboard.",
            ["showleaderboard"] = "Operators only: Show the season's leaderboard.",
            ["cancelseasonleaderboard"] = "Operators only: Cancel the season's leaderboard.",
            ["cancelleaderboard"] = "Operators only: Cancel the season's leaderboard.",
            ["subscriptionadjust"] = "Operators only: Adjust a user's internal subscription count. Also gives them tokens. Arguments: <username> <months> <message>",
            ["subadjust"] = "Operators only: Adjust a user's internal subscription count. Also gives them tokens. Arguments: <username> <months> <message>",
            ["setsubmonths"] = "Operators only: Override the stored number of months subbed. Arguments: <username> <number of months>",
            ["adjustxp"] = "Operators only: Add or remove exp. Will cause users to have negative exp, but not delevel. Arguments: <username> <amount of exp>",
            ["adjustexp"] = "Operators only: Add or remove exp. Will cause users to have negative exp, but not delevel. Arguments: <username> <amount of exp>",
            ["deputise"] = "Operators only: Assign a user to be someone else deputy. Arguments: <user to be sheriff>, <user to be deputy>",
            ["undeputise"] = "Operators only: Remove someones deputy status from someone else. Arguments: <user to be sheriff>, <user to be deputy>",
            ["announcements"] = "Operators only: Add, update or remove an announcement that shows up at each break. Arguments: <option>(add(insert)/list/update/remove(delete)) <announcement_id>(optional: must be placed when when using update/remove option) <duration>(s(seconds)/m(minutes)/h(hours)/d(days)/w(weeks))(optional: use this to give an expiration date to a given announcement) <message>(optional: used when adding/updating an announcement",
            ["announcement"] = "Operators only: Add, update or remove an announcement that shows up at each break. Arguments: <option>(add(insert)/list/update/remove(delete)) <announcement_id>(optional: must be placed when when using update/remove option) <duration>(s(seconds)/m(minutes)/h(hours)/d(days)/w(weeks))(optional: use this to give an expiration date to a given announcement) <message>(optional: used when adding/updating an announcement",
            ["announce"] = "Operators only: Add, update or remove an announcement that shows up at each break. Arguments: <option>(add(insert)/list/update/remove(delete)) <announcement_id>(optional: must be placed when when using update/remove option) <duration>(s(seconds)/m(minutes)/h(hours)/d(days)/w(weeks))(optional: use this to give an expiration date to a given announcement) <message>(optional: used when adding/updating an announcement",
            ["anotherinput"] = "Operators only: Trigger an additional sidegame input.",
            ["overridematchresult"] = "Operators only: Override the result of a past match. Arguments: <datetime of match 'YYYY-MM-DD HH:MM', or match ID>, <blue, red or draw>",
            ["whitelist"] = "Operators only: Add or remove a user in the API whitelist. Argument: <username>",
            ["unwhitelist"] = "Operators only: Add or remove a user in the API whitelist. Argument: <username>",
            ["liquidate"] = "Operators only: Liquidate a user, giving their items, badges and tokens to other users randomly. Arguments: <username> <winners_active_since>(optional date) <confirmation code>(optional, use to confirm liquidation)",
            ["itemliquidate"] = "Operators only: Liquidate a user's items, giving them to other users randomly. Arguments: <username> <winners_active_since>(optional date) <confirmation code>(optional, use to confirm liquidation)",
            ["createbadge"] = "Operators only: Create a badge for a user. Arguments: <username> <Pokémon>",
            ["distributebadge"] = "Operators only: Give a badge out at random to inputters at a certain time. Arguments: <Pokémon> <datetime> <count>",
            ["removebadge"] = "Operators only: Remove a badge from a user's inventory. Arguments: <username> <Pokémon>",
            ["transferbadge"] = "Operators only: Move a badge from one user to another. Arguments: <Pokémon> <username to take from> <username to give to>",
            ["forceacceptbadge"] = "Operators only: Add a copy of every Gen 1 badge to your account. For debugging.",
            ["forcetransmuteupgrade"] = "Operators only: Add a new gen to transmute. Deprecated.",
            ["transmuteraritytest"] = "Operators only: Test transmute rarity differences. Argument: <rarity>",
            ["createitem"] = "Operators only: Give a user some number of items. Arguments: <username> <itemname> <number of items> <itemdata>(optional)",
            ["togglemarket"] = "Operators only: Close or reopen the item market.",
            ["giveavataritem"] = "Operators only: Give avatar clothing options to a user. Arguments: <user name> <avatar style id> <clothing category> <item id>.",
            ["createavataritem"] = "Operators only: Give avatar clothing options to a user. Arguments: <user name> <avatar style id> <clothing category> <item id>.",
            ["giveclothing"] = "Operators only: Give avatar clothing options to a user. Arguments: <user name> <avatar style id> <clothing category> <item id>.",
            ["createclothing"] = "Operators only: Give avatar clothing options to a user. Arguments: <user name> <avatar style id> <clothing category> <item id>.",
            ["unlockclothing"] = "Operators only: Give avatar clothing options to a user. Arguments: <user name> <avatar style id> <clothing category> <item id>.",
            ["unlockavataritem"] = "Operators only: Give avatar clothing options to a user. Arguments: <user name> <avatar style id> <clothing category> <item id>.",
            ["queuegame"] = "Operators only: Select the next game to be played in roulette mode. Argument: <game name>",
            ["testcheerfulslots"] = "Operators only: Run cheerful slots, for debugging.",
            ["makestreamlabsdonation"] = "Operators only: Perform a test donation. Arguments: <username> <amount> <message>",
            ["ircline"] = "Operators only: Process the provided text as a line received over IRC.",
            ["reloadmatchmaker"] = "Operators only: Reload matchmaker from configs. Argument: <matchmaker event>(optional, loads the currently configured event if not specified)",

            // moderator commands
            ["ban"] = "Moderators only: Ban a user. This bot ignores the banned user. Bans remove any existing timeouts. Arguments: <username> <reason>",
            ["unban"] = "Moderators only: Unban a user. Unbans remove any existing timeouts. Arguments: <username> <reason>",
            ["checkban"] = "Moderators only: Check if a user is banned. Argument: <username>",
            ["timeout"] = "Moderators only: Timeout a user. This bot ignores the user until the timeout ends. Arguments: <username> <duration>(ex: 2d / 10m / 1w2d5h10m30s) <reason>",
            ["untimeout"] = "Moderators only: End a user's timeout. Arguments: <username> <reason>",
            ["checktimeout"] = "Moderators only: Check if a user was manually timed out. Argument: <username>",
            ["clearcatchphrases"] = "Moderators only: Clear a user's avatar catchphrases, Argument: <user to clear>",
            ["clearavatarphrases"] = "Moderators only: Clear a user's avatar catchphrases, Argument: <user to clear>",
            ["clearavatarcatchphrases"] = "Moderators only: Clear a user's avatar catchphrases, Argument: <user to clear>",
            ["clearphrases"] = "Moderators only: Clear a user's avatar catchphrases, Argument: <user to clear>",
            ["clearavatarnames"] = "Moderators only: Clears a user's avatar names. Argument: <user to clear>",
            ["clearavatarname"] = "Moderators only: Clears a user's avatar names. Argument: <user to clear>",
            ["cancelmatch"] = "Moderators only: End the current match in a draw. Argument: hard(optional, will restart the emulator instead of triggering a forfeit in-game)",
            ["restartemulator"] = "Moderators only: Restart the match emulator. If a match is playing, this loads the same match on the emulator again without cancelling it. Only affects the emulator.",
            ["restartemu"] = "Moderators only: Restart the match emulator. If a match is playing, this loads the same match on the emulator again without cancelling it. Only affects the emulator.",
            ["resetmatch"] = "Moderators only: Restart the match emulator. If a match is playing, this loads the same match on the emulator again without cancelling it. Only affects the emulator.",
            ["restartmatch"] = "Moderators only: Restart the match emulator. If a match is playing, this loads the same match on the emulator again without cancelling it. Only affects the emulator.",
            ["resetemu"] = "Moderators only: Restart the match emulator. If a match is playing, this loads the same match on the emulator again without cancelling it. Only affects the emulator.",
            ["fov"] = "Moderators only: Change PBR's FOV for the current match only. Argument: <new FOV>(can be decimal. Optional: replies with current value if omitted)",
            ["setfov"] = "Moderators only: Change PBR's FOV for the current match only. Argument: <new FOV>(can be decimal. Optional: replies with current value if omitted)",
            ["fieldeffectstrength"] = "Moderators only: Change the strength of special effects in PBR for the current match only. Default is 1.0. Argument: <new multiplier>(can be decimal. Optional: replies with current value if omitted)",
            ["setfieldeffectstrength"] = "Moderators only: Change the strength of special effects in PBR for the current match only. Default is 1.0. Argument: <new multiplier>(can be decimal. Optional: replies with current value if omitted)",
            ["setweathereffectstrength"] = "Moderators only: Change the strength of special effects in PBR for the current match only. Default is 1.0. Argument: <new multiplier>(can be decimal. Optional: replies with current value if omitted)",
            ["animstrength"] = "Moderators only: Change the strength of animation effects (moves, weather, flames, confetti) in PBR for the current match only. Default is 1.0. 0.5 behaves like 0. Argument: <new value>(can be decimal. Optional: replies with current value if omitted)",
            ["setanimstrength"] = "Moderators only: Change the strength of animation effects (moves, weather, flames, confetti) in PBR for the current match only. Default is 1.0. 0.5 behaves like 0. Argument: <new value>(can be decimal. Optional: replies with current value if omitted)",
            ["setanimationstrength"] = "Moderators only: Change the strength of animation effects (moves, weather, flames, confetti) in PBR for the current match only. Default is 1.0. 0.5 behaves like 0. Argument: <new value>(can be decimal. Optional: replies with current value if omitted)",
            ["emuspeed"] = "Moderators only: Set PBR's emulation speed for the current match only. Argument: <new speed multiplier>(can be decimal. Optional: replies with current value if omitted)",
            ["setemuspeed"] = "Moderators only: Set PBR's emulation speed for the current match only. Argument: <new speed multiplier>(can be decimal. Optional: replies with current value if omitted)",
            ["setemulationspeed"] = "Moderators only: Set PBR's emulation speed for the current match only. Argument: <new speed multiplier>(can be decimal. Optional: replies with current value if omitted)",
            ["animspeed"] = "Moderators only: Set PBR's animation speed for the current match only. Argument: <new speed multiplier>(must be between 1.0 and 2.0. Can be decimal. Optional: replies with current value if omitted)",
            ["setanimspeed"] = "Moderators only: Set PBR's animation speed for the current match only. Argument: <new speed multiplier>(must be between 1.0 and 2.0. Can be decimal. Optional: replies with current value if omitted)",
            ["setanimationspeed"] = "Moderators only: Set PBR's animation speed for the current match only. Argument: <new speed multiplier>(must be between 1.0 and 2.0. Can be decimal. Optional: replies with current value if omitted)",
            ["inputdelay"] = "Moderators only: Set both PBR's regular and switch-only selection delay for the current match only. Argument: <delay in seconds>(can be decimal. Optional: replies with current values if omitted)",
            ["selectiondelay"] = "Moderators only: Set both PBR's regular and switch-only selection delay for the current match only. Argument: <delay in seconds>(can be decimal. Optional: replies with current values if omitted)",
            ["setinputdelay"] = "Moderators only: Set both PBR's regular and switch-only selection delay for the current match only. Argument: <delay in seconds>(can be decimal. Optional: replies with current values if omitted)",
            ["setselectiondelay"] = "Moderators only: Set both PBR's regular and switch-only selection delay for the current match only. Argument: <delay in seconds>(can be decimal. Optional: replies with current values if omitted)",
            ["regulardelay"] = "Moderators only: Set PBR's regular selection delay for the current match only. Argument: <delay in seconds>(can be decimal. Optional: replies with current value if omitted)",
            ["switchonlydelay"] = "Moderators only: Set PBR's switch-only selection delay for the current match only (ex: after faint, after baton pass, etc). Argument: <delay in seconds>(can be decimal. Optional: replies with current value if omitted)",
            ["suspenduser"] = "Moderators only: Suspend a user. Argument: <username>",
            ["suspend"] = "Moderators only: Suspend a user. Argument: <username>",
            ["unsuspenduser"] = "Moderators only: Undo a suspension on a user. Argument: <username>",
            ["unsuspend"] = "Moderators only: Undo a suspension on a user. Argument: <username>",
            ["markbot"] = "Moderators only: Mark user as a bot. Argument: <user to mark>",
            ["unmarkbot"] = "Moderators only: Unmark a user as a bot. Argument: <user to unmark>",
            ["checkbot"] = "Moderators only: Check if a user is marked as a bot. Argument: <user to check>",
            ["marknobet"] = "Moderators only: Mark user as unable to bet. Argument: <user to mark>",
            ["unmarknobet"] = "Moderators only: Unmark a user as unable to bet. Argument: <user to unmark>",
            ["checknobet"] = "Moderators only: Check if a user is marked as unable to bet. Argument: <user to check>",
            ["marktrusted"] = "Moderators only: Mark a user as trusted to use certain commands. Argument: <user to mark>",
            ["unmarktrusted"] = "Moderators only: Unmark a user as trusted to use certain commands. Argument: <user to unmark>",
            ["checktrusted"] = "Moderators only: Check if a user is trusted. Argument: <user to check>",
            ["hashtagsclear"] = "Moderators only: Clear the hastags.",
            ["clearhashtags"] = "Moderators only: Clear the hastags.",
            ["hashtagsignore"] = "Moderators only: Cause the hastags to ignore a selected user until next core reset. Argument: <username>",
            ["ignorehashtags"] = "Moderators only: Cause the hastags to ignore a selected user until next core reset. Argument: <username>",
            ["panicrestore"] = "Moderators only: Restore an earlier savestate in the current run.",
            ["panicreset"] = "Moderators only: Restore an earlier savestate in the current run.",
            ["getid"] = "Moderators only: See someones user id, or your own if none is specified. Argument: <username>(Optional)",

            // trusted commands
            ["announcer"] = "Trusted only: Turn the announcer on and off. Takes immediate effect, even mid-battle, and persists for subsequent matches. Argument: \"on\" or \"off\" (optional: replies with current value if omitted)",
            ["setannouncer"] = "Trusted only: Turn the announcer on and off. Takes immediate effect, even mid-battle, and persists for subsequent matches. Argument: \"on\" or \"off\" (optional: replies with current value if omitted)",
            ["nextgame"] = "Trusted only: Move to the next game when in roulette mode.",
            ["enablepbrmusic"] = "Trusted only: Enable PBR's game music for the next match only.",
            ["disablepbrmusic"] = "Trusted only: Disable PBR's game music if it has been queued for the next match.",

            // user information
            ["level"] = "Show your level and experience.",
            ["experience"] = "Show your level and experience.",
            ["exp"] = "Show your level and experience.",
            ["xp"] = "Show your level and experience.",
            ["loyalty"] = "Show your current subscription Loyalty League. See the description for more info about Leagues. Argument: <username> (optional)",
            ["league"] = "Show your current subscription Loyalty League. See the description for more info about Leagues. Argument: <username> (optional)",
            ["seesubmonths"] = "Show total number of months subbed. Arguments: <username>",

            // generic information
            ["getavataruser"] = "See the owners of avatars used in matches. Arguments: Nothing for current match or one of: <datetime of match> (format: YYYY-MM-DD HH:MM), <match ID>",
            ["getavatarusers"] = "See the owners of avatars used in matches. Arguments: Nothing for current match or one of: <datetime of match> (format: YYYY-MM-DD HH:MM), <match ID>",
            ["modes"] = "Provide info on match modes.  Type `!mode <mode name>` for info on a specific mode, or `!modes all` to see all available modes.",
            ["mode"] = "Provide info on match modes.  Type `!mode <mode name>` for info on a specific mode, or `!modes all` to see all available modes.",
            ["song"] = "Bid on songs for PBR. https://pastebin.com/kiR5ijuY for full details.",
            ["music"] = "Bid on songs for PBR. https://pastebin.com/kiR5ijuY for full details.",
            ["avatars"] = "Give the link to the avatars editor",
            ["commands"] = "Show commands for playing the current game.",
            ["leaderboardtext"] = "Provide leaderboard data in text form. Arguments: season, start position, end position(optional, trusted users only)",

            // items
            ["inventory"] = "Show your inventory. Argument: update(optional, updates out of date items to the modern method if provided)",
            ["items"] = "Show your inventory. Argument: update(optional, updates out of date items to the modern method if provided)",
            ["useitem"] = "Use an item from your inventory. Items may require additional Arguments. Argument: <#slot number, or item name without spaces>",
            ["checkitem"] = "See details of a type of item. Argument: <itemname or item number>",
            ["buyitem"] = "Buy an item on the market, or put a buy order up. Arguments: t<tokens> <itemname> <quantity> <duration>(s(seconds)/m(minutes)/h(hours)/d(days)/w(weeks)). Optional: if not provided will not put up a buy order.) <pack size>pack(optional: only needed for items bought and sold in packs)",
            ["listbuyitem"] = "List the items you are buying.",
            ["cancelbuyitem"] = "Cancel a buy order on one or more items. Arguments: <itemname> t<tokens>(optional: cancels only that price if provided) <quantity>(optional: cancels all if not provided) <pack size>pack(optional: only required for items sold in packs)",
            ["sellitem"] = "Sell an item on the market for tokens. Arguments: t<tokens> <itemnumnber> <quantity> <pack size>pack(optional: only needed for items bought and sold in packs)",
            ["listsellitem"] = "List the items you are selling.",
            ["cancelsellitem"] = "Cancel selling one or more items. Arguments: <itemnumber> t<tokens>(optional: cancels only that price if provided) <quantity>(optional: cancels all if not provided) <pack size>pack(optional: only required for items sold in packs)",
            ["listitemsnotforsale"] = "List the items you are not selling.",
            ["listitemsnotonsale"] = "List the items you are not selling.",
            ["giftitem"] = "Gift an item you own to another user with no price. Arguments: <item> <number of items>(Optional) <username>",

            // badges
            ["checkbadge"] = "View statistics about a badge. Argument: <Pokémon>",
            ["buybadge"] = "Buy a badge that is being sold. If no selling badge matches, it will place a buy order on the badge. Arguments: <Pokémon> <quantity> t<tokens> <duration>(s(seconds)/m(minutes)/h(hours)/d(days)/w(weeks))(optional: will not place a buy order if not specified)",
            ["listbuybadge"] = "List all the badge buy offers you have up.",
            ["cancelbuybadge"] = "Cancel all buy orders up for a particular badge. Argument: <Pokémon>",
            ["sellbadge"] = "Sell a badge you own for tokens. If no buy offer matches it, it will remain on sale until sold. Arguments: <Pokémon> <quantity> t<tokens>",
            ["listsellbadge"] = "List all the badges you are selling.",
            ["cancelsellbadge"] = "Cancel trying to sell a species of badges. Argument: <Pokémon>",
            ["listbadgesnotforsale"] = "List all the badges you have that are not for sale.",
            ["listbadgesnotonsale"] = "List all the badges you have that are not for sale.",

            // misc
            ["storm"] = "Distribute your tokens randomly among players inputting on the sidegame. Argument: t<number of tokens>",
            ["bribe"] = "Place a token bribe on an input on sidegame. These will be distributed to a random person on that input at the end of the next sidegame input. Arguments: <input> t<tokens>",
            ["bribes"] = "See current bribes.",
            ["pinball"] = "Play pinball, or use a subcommand. Details: https://pastebin.com/W4Vrwvmx",
            ["pinballodds"] = "Display the payout rate of pinball in the past timeframe (default 90 days), with a specified score on a specified table. Arguments: <time_to_go_back>(optional), <score>(optional), <table>",
            ["tutorial"] = "Start the tutorial, if it has not already triggered.",
            ["bet"] = "Chat only: bet on PBR using Pokéyen. Arguments: <team> <amount or percent>",
            ["match"] = "Bid your own matches like this: !match <modes> <teams> t1. Modes and teams are optional.  Omit \"t1\" for a dry run.  For more info, type \"!match modes\" or \"!match teams\".",

            // @formatter:on
        };
    }
}
