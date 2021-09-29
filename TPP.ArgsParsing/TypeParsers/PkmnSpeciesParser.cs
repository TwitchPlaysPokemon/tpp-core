using TPP.Common;

namespace TPP.ArgsParsing.TypeParsers;

/// <summary>
/// A parser that recognizes a pokemon species, either by name or by a #-prefixed species id.
/// </summary>
public class PkmnSpeciesParser : IArgumentParser<PkmnSpecies>
{
    private readonly ImmutableDictionary<string, PkmnSpecies> _nameLookup;
    private readonly IImmutableDictionary<string, PkmnSpecies> _idLookup;
    private readonly Func<string, string> _normalizeName;

    /// <summary>
    /// Create a new pkmn species parser for a set of known species.
    /// </summary>
    /// <param name="knownSpecies">all species that should be considered "existing" by the parser</param>
    /// <param name="normalizeName">a function that performs any name normalization</param>
    public PkmnSpeciesParser(
        IEnumerable<PkmnSpecies> knownSpecies,
        Func<string, string>? normalizeName = null)
    {
        _normalizeName = normalizeName ?? (name => name);
        _nameLookup = knownSpecies.ToImmutableDictionary(s => NormalizeName(s.Name), s => s);
        _idLookup = _nameLookup.Values.ToImmutableDictionary(s => s.Id, s => s);
    }

    private string NormalizeName(string name) => _normalizeName(name).ToLowerInvariant();

    public Task<ArgsParseResult<PkmnSpecies>> Parse(IImmutableList<string> args, Type[] genericTypes)
    {
        if (!args[0].StartsWith("#"))
        {
            string normalizedName = NormalizeName(args[0]);
            if (_nameLookup.TryGetValue(normalizedName, out PkmnSpecies? speciesFromName))
            {
                return Task.FromResult(ArgsParseResult<PkmnSpecies>.Success(
                    speciesFromName, args.Skip(1).ToImmutableList()));
            }
            if (args.Count >= 2)
            {
                string normalizedNameTwoArgs = NormalizeName(args[0] + ' ' + args[1]);
                if (_nameLookup.TryGetValue(normalizedNameTwoArgs, out PkmnSpecies? speciesFromTwoArgsName))
                {
                    return Task.FromResult(ArgsParseResult<PkmnSpecies>.Success(
                        speciesFromTwoArgsName, args.Skip(2).ToImmutableList()));
                }
            }
            if (int.TryParse(normalizedName, out _))
            {
                return Task.FromResult(ArgsParseResult<PkmnSpecies>.Failure(
                    "Please prefix with '#' to supply and pokedex number", ErrorRelevanceConfidence.Likely));
            }
            else
            {
                return Task.FromResult(ArgsParseResult<PkmnSpecies>.Failure(
                    $"No pokemon with the name '{normalizedName}' was recognized. " +
                    "Please supply a valid name, or prefix with '#' to supply and pokedex number instead"));
            }
        }
        string speciesId = args[0][1..];
        return Task.FromResult(_idLookup.TryGetValue(speciesId.TrimStart('0'), out var species)
            ? ArgsParseResult<PkmnSpecies>.Success(species, args.Skip(1).ToImmutableList())
            : ArgsParseResult<PkmnSpecies>.Failure($"did not recognize species '{args[0]}'",
                ErrorRelevanceConfidence.Likely)
        );
    }
}
