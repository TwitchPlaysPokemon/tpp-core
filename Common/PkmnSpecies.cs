using System;
using System.Collections.Generic;

namespace Common
{
    /// <summary>
    /// A <see cref="PkmnSpecies"/> uniquely identifies and describes a species of pokemon.
    /// Each species has an <see cref="Id"/>, and additional pokedex information
    /// like the <see cref="Name">name</see> or pokedex <see cref="Flavors">flavor texts</see>.
    /// <para>
    /// Application code should never pass around raw species id strings.
    /// Instead, it is recommended to immediately turn every raw input into an instance of this
    /// class using <see cref="OfId"/> or <see cref="OfIdWithPokedexData"/> and use that instead.
    /// All equality checks safely forward to the species id.
    /// </para>
    /// <para>
    /// This class is deliberately not loading any pokedex data by itself, since that would most
    /// likely be done by loading a large JSON file.
    /// Loading such a file imposes additional startup time, could depend on a JSON library
    /// (e.g. JSON.net) and takes up significant space. Since this class lives in the "Common" project,
    /// which every other project depends on, that is to be avoided.
    /// Instead, the project bundling all dependencies together (the main application)
    /// should register pokedex data using <see cref="RegisterPokedexData"/> at startup.
    /// </para>
    /// </summary>
    public sealed class PkmnSpecies : IComparable<PkmnSpecies>
    {
        private static readonly Dictionary<string, string> NoFlavors = new Dictionary<string, string>();

        /// <summary>
        /// Internal dictionary that keeps track of any registered pokedex data
        /// by providing a lookup from species ids to species instances containing said pokedex data.
        /// </summary>
        private static readonly Dictionary<string, PkmnSpecies> Pokedex = new Dictionary<string, PkmnSpecies>();

        /// <summary>
        /// The species' unique identifier.
        /// Is either a number (for national pokedex) or of the format <c>&lt;number&gt;-&lt;pokedex&gt;</c>.
        /// All equality checks get forwarded to this field.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// The species' displayed name, e.g. 'Pidgey'.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// A mapping from game titles to pokedex flavor texts.
        /// </summary>
        public readonly IDictionary<string, string> Flavors;

        private readonly string _sortKey;
        private readonly string _displayText;

        private PkmnSpecies(string id, string name, IDictionary<string, string> flavors)
        {
            Id = id;
            Name = name;
            Flavors = flavors;

            string[] parts = id.Split("-", count: 2);
            int intPart;
            try
            {
                intPart = int.Parse(parts[0]);
            }
            catch (FormatException)
            {
                throw new ArgumentException(
                    $"The id '{id}' is invalid. " +
                    "It needs to be a number (for national pokedex) or have the format '<number>-<pokedex>'");
            }
            bool isCustomDex = parts.Length > 1;
            _sortKey = isCustomDex
                ? $"{parts[1]}{intPart:00000}"
                : $"_{intPart:00000}"; // prefix with '_' to keep the national dex at the alphanumerical top
            _displayText = isCustomDex
                ? $"#{intPart:000}-{parts[1]} {name}"
                : $"#{intPart:000} {name}";
        }

        /// <summary>
        /// Registers pokedex data for a species, identified by a species id.
        /// Any already registered data for that species gets overwritten.
        /// </summary>
        /// <param name="id">The species' id to add the data for.</param>
        /// <param name="name">The species' displayed name to register.</param>
        /// <param name="flavors">A mapping from game titles to flavor texts to register for the species.</param>
        public static void RegisterPokedexData(string id, string name, IDictionary<string, string> flavors)
        {
            Pokedex[id] = new PkmnSpecies(id, name, flavors);
        }

        /// <summary>
        /// Clears all currently registered pokedex data.
        /// </summary>
        public static void ClearPokedexData()
        {
            Pokedex.Clear();
        }

        /// <summary>
        /// Gets a species instance for the specified species id,
        /// but only if there was pokedex data supplied for that id.
        /// If there wasn't, null is returned.
        /// </summary>
        /// <param name="id">species id to search for.</param>
        /// <returns>species instance, or null if there isn't any pokedex data.</returns>
        public static PkmnSpecies? OfIdWithPokedexData(string id)
        {
            return Pokedex.TryGetValue(id, out PkmnSpecies? species)
                ? species
                : null;
        }

        /// <summary>
        /// Gets a species instance for the specified species id.
        /// This assumes a species with the supplied id should exist
        /// and always returns a valid pokemon species instance.
        /// If no pokedex data was registered for that id, some dummy defaults are used.
        /// </summary>
        /// <param name="id">species id to search for.</param>
        /// <returns>species instance.</returns>
        public static PkmnSpecies OfId(string id)
        {
            return Pokedex.TryGetValue(id, out PkmnSpecies? species)
                ? species
                : new PkmnSpecies(id, "???", NoFlavors);
        }

        /// <summary>
        /// Compares species by their pokedex number and pokedex name.
        /// Default pokemon (national pokedex), will appear before any custom pokedex.
        /// After being grouped by pokedex, each pokemon gets sorted by their pokedex number,
        /// in ascending order.
        /// </summary>
        /// <param name="other">other species to compare this one to.</param>
        /// <returns>a number describing which one was greater.
        /// Negative if the other one was greater, positive if this one was greater,
        /// and zero if both were equal.</returns>
        public int CompareTo(PkmnSpecies? other)
        {
            return string.Compare(_sortKey, other?._sortKey, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares for equality. Equality is determined using the <see cref="Id"/> property.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Id == ((PkmnSpecies) obj).Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(PkmnSpecies? species1, PkmnSpecies? species2)
        {
            return ReferenceEquals(species1, species2) ||
                   !ReferenceEquals(species1, null) && !ReferenceEquals(null, species2) && species1.Equals(species2);
        }

        public static bool operator !=(PkmnSpecies? species1, PkmnSpecies? species2)
            => !(species1 == species2);

        /// <summary>
        /// Returns a human readable string representing this species.
        /// It consists of the species' Id and Name.
        /// </summary>
        /// <returns>prettified string representation</returns>
        public override string ToString() => _displayText;
    }
}
