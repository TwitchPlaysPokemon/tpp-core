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

* [.NET Core SDK v2.0+](https://www.microsoft.com/net/download)
* An IDE supporting C# development such as [Visual Studio Code](https://code.visualstudio.com/Download?wt.mc_id=DotNet_Home) (free & open-source, cross-platform), [Visual Studio](https://www.visualstudio.com/) (free, Windows), or [JetBrains Rider](https://www.jetbrains.com/rider/) (paid, cross-platform).
* [Git](https://git-scm.com/downloads) (and additional GUI tools if not using command line)

Once you have those tools installed,

1. Clone the repository: `git clone git@github.com:TwitchPlaysPokemon/tpp-core.git`
2. If using an IDE, load the source. Otherwise, switch into the directory, ie: `cd tpp-core`
3. If are not using an IDE that does this for you, install the library dependencies: `dotnet restore`
4. Run the unit tests in all projects: `dotnet test`
5. Build all the projects: `dotnet build`
6. Run a specific C# project (CSPROJ file) in the source where PROJECT_NAME is the name of the project: `dotnet run PROJECT_NAME`

If your IDE does not support SLN files, use `dotnet sln` to manage the file to keep it in sync with the CSPROJ files in the repository. This file is for use with Visual Studio.

TODO: tell the user about the example programs once they are written to show the architecture.

## Running

TODO: describe how to run things

## I want to contribute

Yes, please! Any help is appreciated.
Because this project is still very fresh and barebones, it might be hard for you to find a good place to start though.
Depending on your workflow, you can do one of the following:
- Contact one of the core contributors to get invited into the TPP Developer Discord, where you can get directly involved into relevant discussions.
  We are willing to help you get started, just hit someone up. If you don't know where, a Twitch whisper works. Preferably, whisper Chaos_lord2.
- If you already know what you want to add/change, feel free to just tinker around and submit a pull request.
- We'll try to organize things that need to be done as Github issues. You can browse through them and see if something fits for you.
  Don't be shy to ask for help if necessary. You don't need to do something all by yourself to get started.
