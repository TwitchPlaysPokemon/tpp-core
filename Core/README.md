This is the core executable project that ties all components together.
You can set yourself up and run it,
but running the core does nothing yet and is under active development.

## run
First, ensure the project builds and runs on your system by executing it once.
For example, to see all command line options, do
```
dotnet run -- --help
```
Then, generate a config file from default values
```
dotnet run -- gendefaultconfig --outfile=config.json
```
You need to customize a few configurations:
- `Irc.Username` and `Irc.Password` contain the credentials of some Twitch account that will be the chat bot.
   You can obtain an oauth token from [here](https://twitchapps.com/tmi/)

All unchanged entries can be deleted. Missing configurations revert to their default value.

Ensure that you have a properly configured MongoDB server running.
See the [Persistence.MongoDB](../Persistence.MongoDB) project for instructions.

Finally, you run the project with
```
dotnet run -- run
```

## faster startup time
`dotnet run` is a development command that always implicitly restores dependencies,
builds the project, and then executes it. If you want faster startup times,
[publish](https://docs.microsoft.com/en-us/dotnet/core/deploying/) the project first.
For example, making a release build and running the resulting `dll` with the dotnet runtime may look like this:
```
dotnet publish -c Release
dotnet ./bin/Release/netcoreapp3.1/Core.dll --help
```
