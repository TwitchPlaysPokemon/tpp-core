using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;

namespace ArgsParsing.TypeParsers
{
    /// <summary>
    /// A parser that recognizes a pokemon species, either by name or by a #-prefixed species id.
    /// Note that this class builds a name lookup at construction,
    /// so make sure all pokemon species names are set up before instantiating this parser.
    /// For details on that, see <see cref="PkmnSpecies.RegisterName"/>.
    /// </summary>
    public class PkmnSpeciesParser : BaseArgumentParser<PkmnSpecies>
    {
        private readonly ImmutableSortedDictionary<string, PkmnSpecies> _lookup;

        public PkmnSpeciesParser()
        {
            _lookup = PkmnSpecies.AllCurrentlyKnownSpecies.ToImmutableSortedDictionary(s => s.Name.ToLower(), s => s);
        }

        public override Task<ArgsParseResult<PkmnSpecies>> Parse(IImmutableList<string> args, Type[] genericTypes)
        {
            if (!args[0].StartsWith("#"))
            {
                // TODO Add some kind of name normalization to the lookup.
                // TODO That's currently done by pokecat in the old core, so it will likely be some library.
                string normalizedName = args[0].ToLower();
                if (_lookup.TryGetValue(normalizedName, out PkmnSpecies speciesFromName))
                {
                    return Task.FromResult(ArgsParseResult<PkmnSpecies>.Success(
                        speciesFromName, args.Skip(1).ToImmutableList()));
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
            string speciesId = args[0].Substring(startIndex: 1);
            PkmnSpecies? species = PkmnSpecies.OfIdWithKnownName(speciesId.TrimStart('0'));
            return Task.FromResult(species == null
                ? ArgsParseResult<PkmnSpecies>.Failure($"did not recognize species '{args[0]}'",
                    ErrorRelevanceConfidence.Likely)
                : ArgsParseResult<PkmnSpecies>.Success(species, args.Skip(1).ToImmutableList()));
        }
    }
}
