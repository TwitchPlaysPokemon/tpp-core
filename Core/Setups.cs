using System.Collections.Generic;
using System.Linq;
using ArgsParsing;
using ArgsParsing.TypeParsers;
using Core.Commands;
using Core.Commands.Definitions;
using Microsoft.Extensions.Logging;

namespace Core
{
    /// <summary>
    /// Bundling up boilerplate code required to construct various classes.
    /// </summary>
    public static class Setups
    {
        public static ArgsParser SetUpArgsParser()
        {
            var argsParser = new ArgsParser();
            argsParser.AddArgumentParser(new IntParser());
            argsParser.AddArgumentParser(new StringParser());
            argsParser.AddArgumentParser(new InstantParser());
            argsParser.AddArgumentParser(new TimeSpanParser());
            argsParser.AddArgumentParser(new HexColorParser());
            argsParser.AddArgumentParser(new PokeyenParser());
            argsParser.AddArgumentParser(new TokensParser());
            argsParser.AddArgumentParser(new PkmnSpeciesParser());

            argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
            argsParser.AddArgumentParser(new OneOfParser(argsParser));
            argsParser.AddArgumentParser(new OptionalParser(argsParser));

            // TODO add UserParser when its needed
            // argsParser.AddArgumentParser(new UserParser(userRepo));
            return argsParser;
        }

        public static CommandProcessor SetUpCommandProcessor(ILoggerFactory loggerFactory, ArgsParser argsParser)
        {
            var commandProcessor = new CommandProcessor(loggerFactory.CreateLogger<CommandProcessor>(), argsParser);

            IEnumerable<Command> commands = Enumerable.Concat(
                new EasterEggCommands().Commands,
                new StaticResponseCommands().Commands
            );
            foreach (Command command in commands)
            {
                commandProcessor.InstallCommand(command);
            }
            return commandProcessor;
        }
    }
}
