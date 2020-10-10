This is the core executable project that ties all components together.

You can set yourself up and run it to connect to twitch chat
and execute a bunch of basic commands, like `!reddit` or `!stop`.
More elaborate features are still under active development.

## run
First, ensure the project builds and runs on your system by executing it once.
For example, to see all command line options, do
```
dotnet run -- --help
```
Then, try to test your yet non-existing config files using `testconfigs`.
It will tell you what files you need and how to create them.
```
dotnet run -- testconfigs
```
You need to customize a few configurations:
- `Chat.Username` and `Chat.Password` contain the credentials of some Twitch account that will be the chat bot.
   You can obtain an oauth token from [here](https://twitchapps.com/tmi/)
- Add your name to `Chat.OperatorNames` to be able to do stuff requiring elevated privileges,
  for example issuing the !stop command.
- No chat messages are actually being sent by default.
  To change this, set `Chat.Channel` to some unpopulated twitch channel, preferably the bot's
  own channel, and add the channel name to `Chat.SuppressionOverrides`.
  You may also add your own name to `Chat.SuppressionOverrides` to be able to receive whispers.

All unchanged entries can be deleted. Missing configurations revert to their default value.

Ensure that you have a properly configured MongoDB server running.
See the [Persistence.MongoDB](../Persistence.MongoDB) project for instructions.

Finally, you run the project, e.g. in runmode:
```
dotnet run -- runmode
```

## faster startup time
`dotnet run` is a development command that always implicitly restores dependencies,
builds the project, and then executes it. If you want faster startup times,
[publish](https://docs.microsoft.com/en-us/dotnet/core/deploying/) the project first.
For example, making a release build and running the resulting `dll` with the dotnet runtime may look like this:
```
dotnet publish -c Release
dotnet ./bin/Release/net5.0/Core.dll --help
```
