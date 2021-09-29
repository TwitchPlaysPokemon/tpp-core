using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using TPP.Common;

namespace TPP.Model;
// This file contains POCOs for generic pokemon related data that adhere to the
// [pokecat specification](https://github.com/TwitchPlaysPokemon/pokecat/blob/master/unified_objects.md)
// and are therefore also suitable for exchange between systems.
// This includes proper serialization, if the serializer used recognizes data contract annotations
// (e.g. JSON.net does by default).

[DataContract]
public readonly struct Ability
{
    [DataMember(Name = "id")] public int Id { get; init; }
    [DataMember(Name = "name")] public string Name { get; init; }
    [DataMember(Name = "description")] public string Description { get; init; }
}

[DataContract]
public enum Category
{
    [EnumMember(Value = "Physical")] Physical,
    [EnumMember(Value = "Special")] Special,
    [EnumMember(Value = "Status")] Status,
}

[DataContract]
public enum Gender
{
    [EnumMember(Value = "m")] Male,
    [EnumMember(Value = "f")] Female,
}

[DataContract]
public readonly struct Item
{
    [DataMember(Name = "id")] public int Id { get; init; }
    [DataMember(Name = "name")] public string Name { get; init; }
    [DataMember(Name = "description")] public string Description { get; init; }
}

[DataContract]
public readonly struct Move
{
    [DataMember(Name = "id")] public int Id { get; init; }
    [DataMember(Name = "name_id")] public string NameId { get; init; }
    [DataMember(Name = "displayname")] public string Name { get; init; }
    [DataMember(Name = "category")] public Category Category { get; init; }
    [DataMember(Name = "type")] public PokemonType Type { get; init; }
    [DataMember(Name = "accuracy")] public int Accuracy { get; init; }
    [DataMember(Name = "power")] public int Power { get; init; }
    [DataMember(Name = "pp")] public int Pp { get; init; }
    [DataMember(Name = "pp_ups")] public int PpUps { get; init; }
    [DataMember(Name = "target")] public MoveTarget Target { get; init; }
}

[DataContract]
public enum MoveTarget
{
    [EnumMember(Value = "self")] Self,
    [EnumMember(Value = "scripted")] Scripted,
    [EnumMember(Value = "randomNormal")] RandomNormal,
    [EnumMember(Value = "normal")] Normal,
    [EnumMember(Value = "any")] Any,
    [EnumMember(Value = "adjacentFoe")] AdjacentFoe,
    [EnumMember(Value = "foeSide")] FoeSide,
    [EnumMember(Value = "allAdjacentFoes")] AllAdjacentFoes,
    [EnumMember(Value = "allAdjacent")] AllAdjacent,
    [EnumMember(Value = "all")] All,
    [EnumMember(Value = "adjacentAllyOrSelf")] AdjacentAllyOrSelf,
    [EnumMember(Value = "adjacentAlly")] AdjacentAlly,
    [EnumMember(Value = "allyTeam")] AllyTeam,
    [EnumMember(Value = "allySide")] AllySide,
}

[DataContract]
public readonly struct Nature
{
    [DataMember(Name = "id")] public int Id { get; init; }
    [DataMember(Name = "name")] public string Name { get; init; }
    [DataMember(Name = "increased")] public Stat? Inc { get; init; }
    [DataMember(Name = "decreased")] public Stat? Dec { get; init; }
    [DataMember(Name = "description")] public string Description { get; init; }

    public static readonly Nature Hardy = new() { Id = 0, Name = "Hardy" };
    public static readonly Nature Lonely = new() { Id = 1, Name = "Lonely", Inc = Stat.Atk, Dec = Stat.Def };
    public static readonly Nature Brave = new() { Id = 2, Name = "Brave", Inc = Stat.Atk, Dec = Stat.Spe };
    public static readonly Nature Adamant = new() { Id = 3, Name = "Adamant", Inc = Stat.Atk, Dec = Stat.SpA };
    public static readonly Nature Naughty = new() { Id = 4, Name = "Naughty", Inc = Stat.Atk, Dec = Stat.SpD };
    public static readonly Nature Bold = new() { Id = 5, Name = "Bold", Inc = Stat.Def, Dec = Stat.Atk };
    public static readonly Nature Docile = new() { Id = 6, Name = "Docile" };
    public static readonly Nature Relaxed = new() { Id = 7, Name = "Relaxed", Inc = Stat.Def, Dec = Stat.Spe };
    public static readonly Nature Impish = new() { Id = 8, Name = "Impish", Inc = Stat.Def, Dec = Stat.SpA };
    public static readonly Nature Lax = new() { Id = 9, Name = "Lax", Inc = Stat.Def, Dec = Stat.SpD };
    public static readonly Nature Timid = new() { Id = 10, Name = "Timid", Inc = Stat.Spe, Dec = Stat.Atk };
    public static readonly Nature Hasty = new() { Id = 11, Name = "Hasty", Inc = Stat.Spe, Dec = Stat.Def };
    public static readonly Nature Serious = new() { Id = 12, Name = "Serious" };
    public static readonly Nature Jolly = new() { Id = 13, Name = "Jolly", Inc = Stat.Spe, Dec = Stat.SpA };
    public static readonly Nature Naive = new() { Id = 14, Name = "Naive", Inc = Stat.Spe, Dec = Stat.SpD };
    public static readonly Nature Modest = new() { Id = 15, Name = "Modest", Inc = Stat.SpA, Dec = Stat.Atk };
    public static readonly Nature Mild = new() { Id = 16, Name = "Mild", Inc = Stat.SpA, Dec = Stat.Def };
    public static readonly Nature Quiet = new() { Id = 17, Name = "Quiet", Inc = Stat.SpA, Dec = Stat.Spe };
    public static readonly Nature Bashful = new() { Id = 18, Name = "Bashful" };
    public static readonly Nature Rash = new() { Id = 19, Name = "Rash", Inc = Stat.SpA, Dec = Stat.SpD };
    public static readonly Nature Calm = new() { Id = 20, Name = "Calm", Inc = Stat.SpD, Dec = Stat.Atk };
    public static readonly Nature Gentle = new() { Id = 21, Name = "Gentle", Inc = Stat.SpD, Dec = Stat.Def };
    public static readonly Nature Sassy = new() { Id = 22, Name = "Sassy", Inc = Stat.SpD, Dec = Stat.Spe };
    public static readonly Nature Careful = new() { Id = 23, Name = "Careful", Inc = Stat.SpD, Dec = Stat.SpA };
    public static readonly Nature Quirky = new() { Id = 24, Name = "Quirky" };
}

[DataContract]
public readonly struct Pokemon
{
    [DataMember(Name = "displayname")] public string Name { get; init; }
    [DataMember(Name = "setname")] public string Setname { get; init; }
    [DataMember(Name = "species")] public Species Species { get; init; }
    [DataMember(Name = "nature")] public Nature? Nature { get; init; }
    [DataMember(Name = "ability")] public Ability Ability { get; init; }
    [DataMember(Name = "level")] public int Level { get; init; }
    [DataMember(Name = "gender")] public Gender? Gender { get; init; }
    [DataMember(Name = "shiny")] public bool Shiny { get; init; }
    [DataMember(Name = "happiness")] public int Happiness { get; init; }
    [DataMember(Name = "item")] public Item Item { get; init; }
    [DataMember(Name = "ball")] public Item? Ball { get; init; }
    [DataMember(Name = "form")] public int Form { get; init; }
    [DataMember(Name = "moves")] public IImmutableList<Move> Moves { get; init; }
    [DataMember(Name = "hp_type")] public PokemonType? HpType { get; init; }
    [DataMember(Name = "evs")] public Stats Evs { get; init; }
    [DataMember(Name = "ivs")] public Stats Ivs { get; init; }
    [DataMember(Name = "stats")] public Stats Stats { get; init; }
    [DataMember(Name = "curr_hp")] public int CurrHp { get; init; }
    [DataMember(Name = "status")] public Status Status { get; init; }
}

/// Purposely not just named "Type" to avoid confusion with System.Type
[DataContract]
public enum PokemonType
{
    [EnumMember(Value = "Normal")] Normal,
    [EnumMember(Value = "Fire")] Fire,
    [EnumMember(Value = "Water")] Water,
    [EnumMember(Value = "Electric")] Electric,
    [EnumMember(Value = "Grass")] Grass,
    [EnumMember(Value = "Ice")] Ice,
    [EnumMember(Value = "Fighting")] Fighting,
    [EnumMember(Value = "Poison")] Poison,
    [EnumMember(Value = "Ground")] Ground,
    [EnumMember(Value = "Flying")] Flying,
    [EnumMember(Value = "Psychic")] Psychic,
    [EnumMember(Value = "Bug")] Bug,
    [EnumMember(Value = "Rock")] Rock,
    [EnumMember(Value = "Ghost")] Ghost,
    [EnumMember(Value = "Dragon")] Dragon,
    [EnumMember(Value = "Dark")] Dark,
    [EnumMember(Value = "Steel")] Steel,
    [EnumMember(Value = "Fairy")] Fairy,
    [EnumMember(Value = "???")] QuestionMarks,
}

public static class PokemonTypeExtensions
{
    public static string? GetTypeName(this PokemonType type) => type.GetEnumMemberValue();
}

[DataContract]
public readonly struct Species
{
    [DataMember(Name = "id")] public int Id { get; init; }
    [DataMember(Name = "name")] public string Name { get; init; }
    [DataMember(Name = "basestats")] public Stats Basestats { get; init; }
    [DataMember(Name = "types")] public IList<string> Types { get; init; }
    // optional additional pokedex data
    [DataMember(Name = "color")] public string? Color { get; init; }
    [DataMember(Name = "gender_ratios")] public IList<float>? GenderRatios { get; init; }
}

[DataContract]
public enum Stat
{
    [EnumMember(Value = "hp")] Hp,
    [EnumMember(Value = "atk")] Atk,
    [EnumMember(Value = "def")] Def,
    [EnumMember(Value = "spA")] SpA,
    [EnumMember(Value = "spD")] SpD,
    [EnumMember(Value = "spe")] Spe,
}

[DataContract]
public readonly struct Stats
{
    [DataMember(Name = "hp")] public int Hp { get; init; }
    [DataMember(Name = "atk")] public int Atk { get; init; }
    [DataMember(Name = "def")] public int Def { get; init; }
    [DataMember(Name = "spA")] public int SpA { get; init; }
    [DataMember(Name = "spD")] public int SpD { get; init; }
    [DataMember(Name = "spe")] public int Spe { get; init; }
}

[DataContract]
public readonly struct NonvolatileStatus
{
    [DataMember(Name = "brn")] public bool Burn { get; init; }
    [DataMember(Name = "frz")] public bool Freeze { get; init; }
    [DataMember(Name = "par")] public bool Paralysis { get; init; }
    [DataMember(Name = "psn")] public bool Poison { get; init; }
    [DataMember(Name = "tox")] public int BadPoison { get; init; }
    [DataMember(Name = "slp")] public int Sleep { get; init; }
}

[DataContract]
public struct Status
{
    [DataMember(Name = "nonvolatile")] public NonvolatileStatus Nonvolatile { get; set; }
    // volatile may get added in the future if the need arises
}
