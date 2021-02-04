This project includes the "ArgsParsing" facility, which
-- while theoretically usable on its own -- exists to ease the parsing
of text-based chat commands by declaring a type-safe signature of expected arguments,
instead of requiring commands to manually parse raw input strings.

For example, a chat command that should let users buy badges may take three arguments:
a pokemon species, an amount, and the amount of tokens to pay.
And since the three arguments are not ambiguous, passing them in any order should work too.

Without `ArgsParsing`, the command would need to parse the above information
from the raw text-based input by its own.
With this library parsing may look like this instead:
```c#
// You probably want to construct one ArgsParser instance at startup,
// register all required parsers and then use that instance everywhere.
var argsParser = new ArgsParser();
argsParser.AddArgumentParser(new AnyOrderParser(argsParser));
argsParser.AddArgumentParser(new PkmnSpeciesPAaser());
argsParser.AddArgumentParser(new IntParser());
argsParser.AddArgumentParser(new TokensParser());

var args = ImmutableList.Create("10", "T3", "Pidgey");
var result = await argsParser.Parse<AnyOrder<PkmnSpecies, int, Tokens>>(args);
(PkmnSpecies species, int amount, int tokens) = result;
```

This project already includes parser implementations for a bunch of commonly used types,
but can be extended by deriving from [BaseArgumentParser](IArgumentParser.cs)
and adding that parser to your `ArgsParser`-instance.

Take a look at the [tests](../TPP.ArgsParsing.Tests) for more in-depth usage examples.
