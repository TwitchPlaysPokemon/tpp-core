using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using TPP.Common;

namespace TPP.Core
{
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
            (@"Mime[ -]?Jr\.?",  "Mime Jr."),
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
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            using Stream stream = embeddedProvider.GetFileInfo("Resources/pokemon_names.csv").CreateReadStream();
            using var streamReader = new StreamReader(stream);
            string? line;
            while ((line = streamReader.ReadLine()) != null)
            {
                string[] parts = line.Split(',', count: 2);
                yield return PkmnSpecies.RegisterName(id: parts[0], name: parts[1]);
            }
        }
    }
}
