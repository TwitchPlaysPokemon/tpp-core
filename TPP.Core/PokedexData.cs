using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using TPP.Common;

namespace TPP.Core
{
    public enum Generation
    {
        Gen1 = 1, Gen2, Gen3, Gen4, Gen5, Gen6, Gen7, Gen8, Gen9,
        GenFake = 1000
    }

    public class PokedexData
    {
        public ImmutableSortedSet<PkmnSpecies> KnownSpecies { get; }

        private static readonly (string, string)[] NameNormalizations =
        {
            (@"Nidoran-?[♀f]",   "Nidoran♀"),
            (@"Nidoran-?[♂m]",   "Nidoran♂"),
            (@"Farfetch'?d",     "Farfetch'd"),
            (@"Mr\.?[ -]?Mime",  "Mr. Mime"),
            (@"Ho-?Oh",          "Ho-Oh"),
            (@"Mime[. -]?Jr\.?",  "Mime Jr."),
            (@"Porygon[ -]?Z",   "Porygon-Z"),
            (@"Flab[ée]b[ée]",   "Flabébé"),
            (@"Type:?[ -]?Null", "Type: Null"),
            (@"Jangmo-?o",       "Jangmo-o"),
            (@"Hakamo-?o",       "Hakamo-o"),
            (@"Kommo-?o",        "Kommo-o"),
            (@"Tapu[ -]?Koko",   "Tapu Koko"),
            (@"Tapu[ -]?Lele",   "Tapu Lele"),
            (@"Tapu[ -]?Bulu",   "Tapu Bulu"),
            (@"Tapu[ -]?Fini",   "Tapu Fini"),
            (@"Sirfetch'?d",     "Sirfetch'd"),
            (@"Mr\.?[ -]?Rime",  "Mr. Rime"),
            (@"Great[ -]?Tusk",   "Great Tusk"),
            (@"Brute[ -]?Bonnet", "Brute Bonnet"),
            (@"Walking[ -]?Wake", "Walking Wake"),
            (@"Sandy[ -]?Shocks", "Sandy Shocks"),
            (@"Scream[ -]?Tail",  "Scream Tail"),
            (@"Flutter[ -]?Mane", "Flutter Mane"),
            (@"Slither[ -]?Wing", "Slither Wing"),
            (@"Roaring[ -]?Moon", "Roaring Moon"),
            (@"Iron[ -]?Treads",  "Iron Treads"),
            (@"Iron[ -]?Leaves",  "Iron Leaves"),
            (@"Iron[ -]?Moth",    "Iron Moth"),
            (@"Iron[ -]?Hands",   "Iron Hands"),
            (@"Iron[ -]?Jugulis", "Iron Jugulis"),
            (@"Iron[ -]?Thorns",  "Iron Thorns"),
            (@"Iron[ -]?Bundle",  "Iron Bundle"),
            (@"Iron[ -]?Valiant", "Iron Valiant"),
            (@"Ting-?Lu",         "Ting-Lu"),
            (@"Chien-?Pao",       "Chien-Pao"),
            (@"Wo-?Chien",        "Wo-Chien"),
            (@"Chi-?Yu",          "Chi-Yu"),
            (@"MissingNo\.?",    "MissingNo."),
        };

        private static readonly IImmutableDictionary<Regex, string> NameNormalizationRegexes = NameNormalizations
            .ToImmutableDictionary(
                pair => new Regex($"^{pair.Item1}$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                pair => pair.Item2);

        static PokedexData()
        {
            foreach ((Regex regex, string name) in NameNormalizationRegexes)
                Debug.Assert(regex.IsMatch(name),
                    $"invalid name normalization: '{name}' is not matched by its own regex: {regex}");
        }

        public static string NormalizeName(string name)
        {
            foreach ((Regex regex, string normalizedName) in NameNormalizationRegexes)
                if (regex.IsMatch(name))
                    return normalizedName;
            return name;
        }

        private PokedexData()
        {
            KnownSpecies = LoadSpeciesNames().ToImmutableSortedSet();
        }

        /// Construct an instance with all the pokedex data loaded,
        /// which can be a somewhat expensive operation because it may read data from disk.
        public static PokedexData Load()
        {
            return new PokedexData();
        }

        private static IEnumerable<PkmnSpecies> LoadSpeciesNames()
        {
            const string resourceName = "Resources/pokemon_names.csv";
            using Stream stream = Resources.GetEmbeddedResource(resourceName) ?? throw new Exception(
                $"could not load pokemon data because resource '{resourceName}' is missing");
            using var streamReader = new StreamReader(stream);
            string? line;
            while ((line = streamReader.ReadLine()) != null)
            {
                string[] parts = line.Split(',', count: 2);
                yield return PkmnSpecies.RegisterName(id: parts[0], name: parts[1]);
            }
        }

        private static readonly ImmutableSortedDictionary<Generation, int> GenMaxIds = new Dictionary<Generation, int>
        {
            [Generation.Gen1] = 151, // Mew
            [Generation.Gen2] = 251, // Celebi
            [Generation.Gen3] = 386, // Deoxys
            [Generation.Gen4] = 493, // Arceus
            [Generation.Gen5] = 649, // Genesect
            [Generation.Gen6] = 721, // Volcanion
            [Generation.Gen7] = 807, // Zeraora
            [Generation.Gen8] = 905, // Enamorus
            [Generation.Gen9] = 1018, // Archaludon (for now)
        }.ToImmutableSortedDictionary();

        public static Generation GetGeneration(PkmnSpecies species)
        {
            if (species.IsFakemon) return Generation.GenFake;
            int natId = int.Parse(species.Id);
            foreach ((Generation gen, int maxDexNum) in GenMaxIds)
                if (maxDexNum >= natId)
                    return gen;
            throw new ArgumentException($"{species}'s national pokedex number {natId} is invalid", nameof(species));
        }
    }

    public static class PkmnSpeciesExtensions
    {
        public static Generation GetGeneration(this PkmnSpecies species) => PokedexData.GetGeneration(species);
    }
}
