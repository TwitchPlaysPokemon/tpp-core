This is the core executable project that ties all components together.

You can set yourself up and run it to connect to twitch chat
and execute a bunch of basic commands, like `!reddit` or `!stopnew`.
More elaborate features are still under active development.

## run the project
First, ensure the project builds and runs on your system by executing it once.
For example, to see all command line options, do
```
dotnet run -- --help
```
Then, try to test your yet non-existing base config file using `testconfig`.
It will tell you what file you need and how to create it.
```
dotnet run -- testconfig
```
For local testing, you need to remove all but the `console` entry from the `Chat.Connections` config.
You will be able to use the console as a simulated chat.

If you want to connect to an actual Twitch chat you need to keep the `twitch` entry of the
`Chat.Connections` list and change a few configurations:

1. In the `twitch` entry, `Username` and `Password` contain the
   credentials of some Twitch account that will be the chat bot.
   You can obtain an oauth token from [here](https://twitchapps.com/tmi/).
   These configs are still required because not all chat functionality is migrated to the Twitch API or EventSub yet.
   In the future, you will only need to do 2.) and 3.).

2. In the `twitch` entry, `AppClientId` and `AppClientSecret` contain the credentials of some

   [Twitch Developer Application](https://dev.twitch.tv/console/apps).
   You can create your own application under any account you like.
   If you are TPP staff, this typically is the official "Twitch Plays Pokemon" application,
   which is owned by FelkCraft and has the AppClientId `1iinedru2u5as4ss7pzn237olcrgk2o`.

3. In the `twitch` entry, `RefreshToken` contains the refresh token of some user that authenticated
   against the app configured in 2.), and can be obtained from e.g. [twitchtokengenerator.com](https://twitchtokengenerator.com/).
   You should use the same account as you did in 1.).

   If you are TPP staff, you can also use [twitchplayspokemon.tv/custom_scopes](https://twitchplayspokemon.tv/custom_scopes),
   which uses the official "Twitch Plays Pokemon" application for authenticating.
   For a list of scopes you need to authenticate, see the section "Twitch Authorization Scopes".

4. No chat messages are actually being sent by default.
   To change this, in the `twitch` entry set `Channel` to some unpopulated twitch channel,
   preferably the bot's own channel, and add the channel name to `SuppressionOverrides`.
   You may also add your own name to `SuppressionOverrides` to be able to receive whispers.

5. Add your name to `Chat.DefaultOperatorNames` to be able to do stuff requiring elevated privileges,
   for example issuing the `!stopnew` command.

All unchanged entries can be deleted. Missing configurations revert to their default value.
A minimal configuration may look like this:
```json
{
  "$schema": "./config.schema.json",
  "Chat": {
    "Connections": [{"Type": "console"}]
  }
}
```

Ensure that you have a properly configured MongoDB server running.
See the [TPP.Persistence.MongoDB](../TPP.Persistence.MongoDB) project for instructions.

Finally, you run the project, e.g. in dualcore mode:
```
dotnet run -- start -m dualcore
```

## Twitch Authorization Scopes

The following scopes are currently in use:

| scope                          | used for                                                      |
|--------------------------------|---------------------------------------------------------------|
| chat:read                      | Read messages from chat (via IRC/TMI).                        |
| chat:edit                      | Send messages to chat (via IRC/TMI).                          |
| user:bot                       | Appear in chat as bot.                                        |
| user:read:chat                 | Read messages from chat. (via EventSub)                       |
| user:write:chat                | Send messages to chat. (via Twitch API)                       |
| user:manage:whispers           | Sending and receiving whispers.                               |
| moderator:read:chatters        | Read the chatters list in the channel (e.g. for badge drops). |
| moderator:read:followers       | Read the followers list (currently old core).                 |
| moderator:manage:banned_users  | Timeout, ban and unban users (tpp automod, mod commands).     |
| moderator:manage:chat_messages | Delete chat messages (tpp automod, purge invalid bets).       |
| moderator:manage:chat_settings | Change chat settings, e.g. emote-only mode (mod commands).    |
| channel:read:subscriptions     | Reacting to incoming subscriptions                            |

## modes

You can run different modes by specifying its mode name in the `dotnet run -- start -m <mode>` command.
Some modes require an additional mode-specific config file, but just trying to run them will tell you how to create it.
Currently the following modes are supported:

| mode     | description                                                                                                                             |
|----------|-----------------------------------------------------------------------------------------------------------------------------------------|
| dualcore | This is is a simple mode meant for replacing some functionality that has been ported and subsequently removed from the old python core. |
| match    | This mode runs basic match cycles. You need the old overlay to see anything. See the below section on old overlay compatibility.        |
| run      | This mode processed inputs from chat and offers them for consumption through a http endpoint. It also sends them to the old overlay.    |
| dummy    | This mode purposely does nothing for testing purposes.                                                                                  |

The modes `match` and `run` require a mode-specific configuration file, which you can test and generate
similar to the base config by passing an additional `--mode` or `-m` option.
See the `--help` output for more details on that.

## old overlay compatibility mode

The new core (this repository) does not contain any visual components.
But it does speak the same websocket event dialect the current "old" overlay speaks,
so to see anything at `http://localhost:5000/overlay` the overlay must be started from the old core with `python -m tpp overlay`.
It will then be able to connect to the new core's websocket to receive overlay events.

## proper publishing
`dotnet run` is a development command that always implicitly restores dependencies,
builds the project, and then executes it. Running it this way causes a slow startup
and prevents the application from handling SIGTERM events on linux.

If you want faster startup times or graceful SIGTERM handling,
[publish](https://docs.microsoft.com/en-us/dotnet/core/deploying/) the project first.
For example, making a release build and running the resulting executable may look like this:
```
dotnet publish -c Release
../artifacts/publish/TPP.Core/release/TPP.Core.exe --help
```
