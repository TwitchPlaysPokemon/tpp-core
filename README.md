# TPP Core

![travis-ci build](https://api.travis-ci.org/TwitchPlaysPokemon/tpp-core.svg?branch=master)

This repository contains the open-source rewrite of the TwitchPlaysPokemon core codebase.

Its main goals are the following:

  - Switch to a statically typed language (C#) for more guarantees about code structure and correctness.
  - Use our experiences with the current TPP core to get things _right_, or at least implement them nicer.
  - Be open source to allow contributions from anyone.
  - Drop the frustration dealing with old code, and instead siphon the motivation of doing something fresh

For historical details, see [history](history.md).

*The whole repository, including this document, is still very much under heavy development.
Once we've reached a certain level of development, we will provide more detailed information on how the new codebase is structured, what technologies are used, and what components it consists of.*

## Getting started

You will need

* [.NET Core SDK v2.1+](https://www.microsoft.com/net/download)
* An IDE supporting C# development such as [Visual Studio Code](https://code.visualstudio.com/Download?wt.mc_id=DotNet_Home) (free & open-source, cross-platform), [Visual Studio](https://www.visualstudio.com/) (free, Windows), or [JetBrains Rider](https://www.jetbrains.com/rider/) (paid, cross-platform).
* [Git](https://git-scm.com/downloads) (and additional GUI tools if not using command line)
* [Redis](https://redis.io/download) for pub/sub
* [PostgreSql v9.4+](https://www.postgresql.org/)

Once you have those tools installed,

1. Clone the repository: `git clone git@github.com:TwitchPlaysPokemon/tpp-core.git`
2. If using an IDE, load the source. Otherwise, switch into the directory, ie: `cd tpp-core`
3. If are not using an IDE that does this for you, install the library dependencies: `dotnet restore`
4. Run the unit tests in all projects: `dotnet test`
5. Build all the projects: `dotnet build`
6. Run a specific C# project (CSPROJ file) in the source where PROJECT is the project file: `dotnet run --project PROJECT`. If you need to pass arguments, add `--` then your arguments.
7. Generate portable runtimes (assemblies) for production use: `dotnet publish -c Release`
8. Run the assembly with `dotnet PROJECT.dll`

If your IDE does not support SLN files, use `dotnet sln` to manage the file to keep it in sync with the CSPROJ files in the repository. This file is for use with Visual Studio, MSBuild, code completion, etc.

For more details, such as creating new projects, see the [dotnet command documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet?tabs=netcore2x).


## Source Overview

The core separated into components based on the microservice architecture. Each microservice (or service for short) is intended to run and be able to restart independently. Services communicate directly with each other using RESTful web services (HTTP). Services also communicate indirectly by messages in a pub/sub system.

The source code is organized into several projects according to their usage. For example, `TPPCore.Service.Common` is a project that outputs a library assembly and `TPPCore.Service.Example.Parrot` is a project that outputs a command line program assembly.

The Parrot example service is a simple service intended to demonstrate the basic functionality. It loops infinitely sending messages to the pub/sub client which is a thin wrapper to the Redis client. To query or change the state in service, it exposes simple RESTful HTTP endpoints that accept and send JSON formatted body messages. Internally, it uses the ASP.NET Core framework to start up a Kestrel web host.

To interact with the Parrot service, a corresponding client is provided in `TPPCore.Client.Example.Parrot`. It wraps serialization and deserialization of the JSON messages. To see usage of the client, see the service's test code within the `test` folder.

### Directory structure

| Project | Summary |
| --------- | ------- |
| TPPCore.ChatProviders | Modules to connect to website chat API endpoints |
| TPPCore.Client.Chat | Client to interact with the Chat service |
| TPPCore.Client.Common | Base code for clients that access a service's API |
| TPPCore.Client.Example.Parrot | Client to interact with the Parrot service |
| TPPCore.Irc | Async IRC library |
| TPPCore.Service.Chat | Service for running Chat Providers and providing a unified interface for chatting |
| TPPCore.Service.ChatLogger | Service for logging Chat service events |
| TPPCore.Service.Common | Framework for services |
| TPPCore.Service.Example.Parrot | Example service |


## Running

Services are intended to start up independently from one another.


### How to invoke services

Checking the command line options of the Parrot service:

    dotnet TPPCore.Service.Example.Parrot.dll --help

Running the parrot service with configuration files:

    dotnet TPPCore.Service.Example.Parrot.dll --log-config log4net.config --config service_common_config.json parrot_service_config.json

Note that `--config` switch is not repeated for each configuration filename.

### How to run minimal Twitch Plays

TODO: describe this once it is complete


## I want to contribute

Yes, please! Any help is appreciated.
Because this project is still very fresh and barebones, it might be hard for you to find a good place to start though.
Depending on your workflow, you can do one of the following:
- Join the channel #public-dev on the [TPP community Discord](https://discord.gg/twitchplayspokemon) and contact one of the core contributors. Optionally, you may ask to get invited into the TPP Developer Discord, where you can get directly involved into relevant discussions.
  We are willing to help you get started, just hit someone up. If you don't know where, a Twitch whisper or Discord DM works. Preferably, whisper or DM Chaos_lord2.
- If you already know what you want to add/change, feel free to just tinker around and submit a pull request.
- We'll try to organize things that need to be done as Github issues and project cards. You can browse through them and see if something fits for you.
  Don't be shy to ask for help if necessary. You don't need to do something all by yourself to get started.
