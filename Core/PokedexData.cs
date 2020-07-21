using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Common;
using Microsoft.Extensions.FileProviders;

namespace Core
{
    public class PokedexData
    {
        public IImmutableSet<PkmnSpecies> KnownSpecies { get; }

        private PokedexData()
        {
            KnownSpecies = LoadSpeciesNames().ToImmutableHashSet();
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
