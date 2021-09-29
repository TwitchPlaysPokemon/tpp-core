using System;
using System.Collections.Generic;

namespace TPP.Common;

/// <summary>
/// A <see cref="PkmnSpecies"/> uniquely identifies and describes a species of pokemon.
/// Each species has an <see cref="Id"/>, and additional a  <see cref="Name">name</see>.
/// <para>
/// Application code should never pass around raw species id strings.
/// Instead, it is recommended to immediately turn every raw input into an instance of this
/// class using <see cref="OfId"/> or <see cref="OfIdWithKnownName"/> and use that instead.
/// All equality checks safely forward to the species id.
/// </para>
/// <para>
/// This class is deliberately not loading any pokemon name data by itself,
/// since that would most likely be done by loading a large JSON file in application code.
/// Loading such a file imposes additional startup time, could depend on a JSON library
/// (e.g. JSON.net) and takes up significant space. Since this class lives in the "TPP.Common" project,
/// which every other project depends on, that is to be avoided.
/// Instead, the project bundling all dependencies together (the main application)
/// should register names for known pokemon using <see cref="RegisterName"/> at startup.
/// </para>
/// </summary>
public sealed class PkmnSpecies : IComparable<PkmnSpecies>
{
    /// <summary>
    /// Internal dictionary that keeps track of any existing instances
    /// to ensure only one instance may ever exist per species.
    /// </summary>
    private static readonly Dictionary<string, PkmnSpecies> Instances = new Dictionary<string, PkmnSpecies>();

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
    /// Whether the species is not from the official national pokedex but a custom pokedex, and hence a "fakemon".
    /// </summary>
    public readonly bool IsFakemon;

    private readonly string _sortKey;
    private readonly string _displayText;

    private PkmnSpecies(string id, string name)
    {
        Id = id;
        Name = name;

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
        IsFakemon = parts.Length > 1 || intPart >= 2000; // TODO: Until the 7 fakemons from #2000 - #2006 are dealt with, we need to additionally address them
        _sortKey = parts.Length > 1
            ? $"{parts[1]}{intPart:00000}"
            : $"_{intPart:00000}"; // prefix with '_' to keep the national dex at the alphanumerical top
        _displayText = parts.Length > 1
            ? $"#{intPart:000}-{parts[1]} {name}"
            : $"#{intPart:000} {name}";
    }

    /// <summary>
    /// Registers a name for a species, identified by a species id.
    /// Any already registered name for that species gets overwritten.
    /// </summary>
    /// <param name="id">The species' id to add the data for.</param>
    /// <param name="name">The species' displayed name to register.</param>
    /// <returns>The pkmn species instance for which the name was registered</returns>
    public static PkmnSpecies RegisterName(string id, string name)
    {
        var species = new PkmnSpecies(id, name);
        Instances[id] = species;
        return species;
    }

    /// <summary>
    /// Clears all currently registered pokemon names.
    /// </summary>
    public static void ClearNames()
    {
        Instances.Clear();
    }

    /// <summary>
    /// Gets a species instance for the specified species id,
    /// but only if there was name registered for that id.
    /// If there wasn't, null is returned.
    /// </summary>
    /// <param name="id">species id to search for.</param>
    /// <returns>species instance, or null if no name was registered for the species.</returns>
    public static PkmnSpecies? OfIdWithKnownName(string id)
    {
        return Instances.TryGetValue(id, out PkmnSpecies? species)
            ? species
            : null;
    }

    /// <summary>
    /// Gets a species instance for the specified species id.
    /// This assumes a species with the supplied id should exist
    /// and always returns a valid pokemon species instance.
    /// If no pokemon name was registered for that id, some dummy value is used,
    /// though the returned instance can automatically obtain a proper name afterwards
    /// if one gets supplied for the species with <see cref="RegisterName"/>.
    /// </summary>
    /// <param name="id">species id to search for.</param>
    /// <returns>species instance.</returns>
    public static PkmnSpecies OfId(string id)
    {
        return Instances.TryGetValue(id, out PkmnSpecies? species)
            ? species
            : new PkmnSpecies(id, "???");
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
        return obj.GetType() == GetType() && Id == ((PkmnSpecies)obj).Id;
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
