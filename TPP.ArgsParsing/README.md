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

Take a look at the [tests](../tests/TPP.ArgsParsing.Tests) for more in-depth usage examples.

# built-in types

Parser implementations for the following types are included in this project (their names are `<type>Parser` by convention):

- `HexColor`: 6 hexadecimal digits optionally prefixed with `#`
  ```c#
  HexColor color = await argsParser.Parse<HexColor>(ImmutableList.Create("#ff0000"))
  ```
- `Instant`: UTC instants in ISO-8601 format
  ```c#
  Instant instant = await argsParser.Parse<Instant>(ImmutableList.Create("2014-02-12T15:30:00Z"))
  ```
- `SignedInt`: an unrestricted integer
  ```c#
  int number = await argsParser.Parse<SignedInt>(ImmutableList.Create("-42"))
  ```
- `NonNegativeInt`: an integer >= 0
  ```c#
  int number = await argsParser.Parse<NonNegativeInt>(ImmutableList.Create("0"))
  ```
- `PositiveInt`: an integer > 0
  ```c#
  int number = await argsParser.Parse<PositiveInt>(ImmutableList.Create("42"))
  ```
- `PkmnSpecies`: a pokemon species
  ```c#
  PkmnSpecies species = await argsParser.Parse<PkmnSpecies>(ImmutableList.Create("Pikachu"))
  ```
- `Pokeyen`: a `P`-prefixed nonnegative amount of pokeyen
  ```c#
  int pokeyen = await argsParser.Parse<Pokeyen>(ImmutableList.Create("42"))
  ```
- `SignedPokeyen`: a `P`-prefixed unrestricted amount of pokeyen
  ```c#
  int pokeyen = await argsParser.Parse<SignedPokeyen>(ImmutableList.Create("-42"))
  ```
- `Tokens`: a `T`-prefixed nonnegative amount of tokens
  ```c#
  int tokens = await argsParser.Parse<Tokens>(ImmutableList.Create("42"))
  ```
- `SignedTokens`: a `T`-prefixed unrestricted amount of tokens
  ```c#
  int tokens = await argsParser.Parse<SignedTokens>(ImmutableList.Create("-42"))
  ```
- `string`: the raw input argument
  ```c#
  string arg = await argsParser.Parse<string>(ImmutableList.Create("foo"))
  ```
- `TimeSpan`: a time span in the format `<weeks>w<days>d<hours>h<minutes>m<seconds>s`
  ```c#
  TimeSpan timeSpan = await argsParser.Parse<TimeSpan>(ImmutableList.Create("2m30s"))
  ```
- `User`: an existing TPP user
  ```c#
  User user = await argsParser.Parse<User>(ImmutableList.Create("tppsimulator"))
  ```
- `AnyOrder<T1, T2, ...>`: a set of other types in any order
  ```c#
  (PositiveInt number, string str) = await argsParser.Parse<AnyOrder<PositiveInt, string>>(ImmutableList.Create("abc", "123"))
  ```
- `ManyOf<T>`: a list of some type
  ```c#
  ImmutableList<PkmnSpecies> species = await argsParser.Parse<ManyOf<PkmnSpecies>>(ImmutableList.Create("Pikachu", "Pidgey"))
  ```
- `OneOf<T1, T2, ...>`: a set of other types of which only one must be given
  ```c#
  OneOf<string, PositiveInt> oneOf = await argsParser.Parse<OneOf<string, PositiveInt>>(ImmutableList.Create("foo"))
  ```
- `Optional<T1>`: a type that may be absent
  ```c#
  Optional<string> stringOpt = await argsParser.Parse<Optional<string>>(ImmutableList.Create<string>())
  ```
