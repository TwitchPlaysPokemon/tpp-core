This is the core executable project that ties all components together.

You can set yourself up and run it to connect to twitch chat
and execute a bunch of basic commands, like `!reddit` or `!stop`.
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
You need to customize a few configurations:
- `Chat.Username` and `Chat.Password` contain the credentials of some Twitch account that will be the chat bot.
   You can obtain an oauth token from [here](https://twitchapps.com/tmi/)
- Add your name to `Chat.OperatorNames` to be able to do stuff requiring elevated privileges,
  for example issuing the `!stop` command.
- No chat messages are actually being sent by default.
  To change this, set `Chat.Channel` to some unpopulated twitch channel, preferably the bot's
  own channel, and add the channel name to `Chat.SuppressionOverrides`.
  You may also add your own name to `Chat.SuppressionOverrides` to be able to receive whispers.

All unchanged entries can be deleted. Missing configurations revert to their default value.

Ensure that you have a properly configured MongoDB server running.
See the [Persistence.MongoDB](../Persistence.MongoDB) project for instructions.

Finally, you run the project, e.g. in dualcore mode:
```
dotnet run -- start -m dualcore
```

The `dualcore` mode is a simple mode meant for replacing some functionality
that has been ported and subsequently removed from the old python core.

Some other modes require a mode-specific configuration file, which you can test and generate
similar to the base config by passing an additional `--mode` or `-m` option.
See the `--help` output for more details on that.

## proper publishing
`dotnet run` is a development command that always implicitly restores dependencies,
builds the project, and then executes it. Running it this way causes a slow startup
and prevents the application from handling SIGTERM events on linux.

If you want faster startup times or graceful SIGTERM handling,
[publish](https://docs.microsoft.com/en-us/dotnet/core/deploying/) the project first.
For example, making a release build and running the resulting `dll` with the dotnet runtime may look like this:
```
dotnet publish -c Release
dotnet ./bin/Release/net5.0/Core.dll --help
```
