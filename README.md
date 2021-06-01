
This is a rewrite of the backbone that runs [TwitchPlaysPokemon](https://www.twitch.tv/twitchplayspokemon).
It is replacing the old closed-source Python core with the goal
for improved maintainability and accessability to new contributors.
The scope encompasses, but is not limited to:
- Managing persistent data
- Connecting to Twitch Chat and handling all interactions (e.g. commands, modbot, announcements)
- Processing inputs during runs ("runmode")
- Driving the "match loop" during betting periods ("matchmode")
- Driving the overlay you see on stream
- Driving external programs like emulators

This code is already running live on-stream in 'dual-core' mode,
with all features that are not re-implemented here yet still being
taken care of by the old python core.
Porting features over is an ongoing process.

## requirements
- .NET 6 SDK, you can get one from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- If you use or run any components with persistence, MongoDB.
  See [TPP.Persistence.MongoDB](TPP.Persistence.MongoDB) for details.

## run
The executable project is located at [TPP.Core](TPP.Core). See its readme for further instructions.

## run tests
- `dotnet test` runs all tests
- `dotnet test --filter TestCategory!=IntegrationTest` skips tests that have preconditions or are slow
